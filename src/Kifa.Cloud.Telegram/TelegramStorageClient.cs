using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kifa.IO;
using Kifa.IO.StorageClients;
using Kifa.Threading;
using NLog;
using TL;
using WTelegram;

namespace Kifa.Cloud.Telegram;

public class TelegramStorageClient : StorageClient, CanCreateStorageClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const long ShardSize = 4000L * BlockSize; // 2 GB

    public static int DownloadThread { get; set; } = 8;
    public static int UploadThread { get; set; } = 8;

    public static TimeSpan DownloadTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public static TimeSpan UploadTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public static TimeSpan MergeTimeout { get; set; } = TimeSpan.FromMinutes(10);

    static SemaphoreSlim? downloadTaskSemaphore;
    static SemaphoreSlim DownloadTaskSemaphore => downloadTaskSemaphore ??= new(DownloadThread);
    static SemaphoreSlim? uploadTaskSemaphore;
    static SemaphoreSlim UploadTaskSemaphore => uploadTaskSemaphore ??= new(UploadThread);
    static PriorityLock PriorityLock = new();

    static readonly ConcurrentDictionary<string, TelegramStorageCell> AllCells = new();

    public required string CellId { get; init; }

    public TelegramStorageCell Cell
        => AllCells.GetOrAdd(CellId, cellId => TelegramStorageCell.Client.Get(cellId).Checked());

    TelegramStorageClient() {
    }

    int currentHolders;
    static TelegramCellClient? sharedCellClient;

    TelegramCellClient ObtainCellClient() {
        sharedCellClient ??= Cell.CreateClient();
        currentHolders++;
        return sharedCellClient;
    }

    void ReturnCellClient() {
        currentHolders--;
        if (currentHolders == 0) {
            sharedCellClient?.Release();
            sharedCellClient = null;
        }
    }

    public static StorageClient Create(string spec) {
        if (spec.Contains('*')) {
            var segments = spec.Split("*");
            var client = Create(segments[0]);
            // Sharded client.
            return new ShardedStorageClient {
                Clients = Enumerable.Repeat(client, int.Parse(segments[1])).ToList(),
                ShardSize = ShardSize
            };
        }

        return new TelegramStorageClient {
            CellId = spec
        };
    }

    public static string CreateLocation(FileInformation info, string cell, long encodedSize) {
        // Assume sha256 is present.
        var shardCount = (encodedSize - 1) / ShardSize + 1;
        return shardCount > 1
            ? $"tele:{cell}*{shardCount}/$/{info.Sha256}"
            : $"tele:{cell}/$/{info.Sha256}";
    }

    public override long Length(string path) {
        var document = GetDocument(path);
        if (document == null) {
            throw new FileNotFoundException();
        }

        return document.size;
    }

    public override void Delete(string path) {
        var cellClient = ObtainCellClient();

        try {
            var message = GetMessage(path);
            if (message == null) {
                Logger.Debug($"File {path} is not found.");
                return;
            }

            var result = Retry
                .Run(() => cellClient.Client.DeleteMessages(cellClient.Channel, message.id),
                    HandleFloodExceptionFunc).GetAwaiter().GetResult();
            if (result.pts_count != 1) {
                Logger.Debug($"Delete of {path} is not successful, but is ignored.");
            }
        } finally {
            ReturnCellClient();
        }
    }

    public override void Touch(string path) {
        throw new NotImplementedException();
    }

    const int BlockSize = 1 << 19; // 512 KiB

    public override void Write(string path, Stream stream) {
        if (Exists(path)) {
            return;
        }

        var cellClient = ObtainCellClient();

        try {
            var size = stream.Length;

            // This is to ensure we don't simultaneously read the input when uploading to avoid conflict.
            var uploadInputStreamSemaphore = new SemaphoreSlim(1);

            // size should be at most 1 << 31.
            var totalParts = (int) (size - 1) / BlockSize + 1;
            var fileId = Random.Shared.NextInt64();
            Logger.Debug($"Uploading {path} with temp file id {fileId}...");

            var exceptions = new ConcurrentBag<Exception>();
            var tasks = new Task[totalParts];

            Task GetUploadBlockTask(int i)
                => UploadOneBlock(cellClient, fileId, totalParts, i, stream, i * BlockSize,
                    (int) Math.Min(size - i * BlockSize, BlockSize), exceptions,
                    uploadInputStreamSemaphore);

            for (var i = 0; (long) i * BlockSize < size; ++i) {
                tasks[i] = GetUploadBlockTask(i);
            }

            Task.WhenAll(tasks).GetAwaiter().GetResult();
            if (exceptions.TryPeek(out var ex)) {
                throw ex;
            }

            if (Exists(path)) {
                throw new FileCorruptedException(
                    $"Another program may have uploaded the file {path}. Skip creating the file.");
            }

            var finalResult = Retry.Run(() => cellClient.Client.SendMediaAsync(cellClient.Channel,
                        path, new InputFileBig {
                            id = fileId,
                            parts = totalParts,
                            name = path.Split("/")[^1]
                        }).WaitAsync(MergeTimeout),
                    new Func<Exception, Dictionary<string, int>?, Task<Dictionary<string, int>?>>((
                            exception, failures)
                        => HandleMergeExceptions(exception, failures, GetUploadBlockTask)))
                .GetAwaiter()
                .GetResult();

            if (finalResult.message != path) {
                throw new Exception(
                    $"Failed to upload {path} in the finalization step: {finalResult}.");
            }
        } finally {
            ReturnCellClient();
        }
    }

    async Task UploadOneBlock(TelegramCellClient cellClient, long fileId, int totalParts,
        int partIndex, Stream stream, long fromPosition, int length,
        ConcurrentBag<Exception> exceptions, SemaphoreSlim uploadInputStreamSemaphore) {
        Logger.Trace(
            $"Waiting for uploadSemaphore to uploading part {partIndex} of {totalParts} for {fileId}...");
        await UploadTaskSemaphore.WaitAsync();

        try {
            var buffer = new byte[length];

            await uploadInputStreamSemaphore.WaitAsync();

            try {
                if (!exceptions.IsEmpty) {
                    Logger.Trace("Other task already failed. Fail fast.");
                    return;
                }

                stream.Seek(fromPosition, SeekOrigin.Begin);
                var readLength = await stream.ReadAsync(buffer);
                if (readLength != length) {
                    throw new FileCorruptedException(
                        $"Unexpected read length {readLength}, expecting {length}");
                }
            } catch (IOException ex) {
                Logger.Error(ex,
                    $"Failed to get the input stream from {fromPosition} to upload part {partIndex} of {totalParts} for {fileId}.");
                exceptions.Add(ex);
                return;
            } finally {
                uploadInputStreamSemaphore.Release();
            }

            if (!exceptions.IsEmpty) {
                Logger.Debug("Other task already failed. Fail fast.");
                return;
            }

            Logger.Trace($"Uploading part {partIndex} of {totalParts} for {fileId}...");
            var partResult = await Retry.Run(async () => {
                Logger.Trace(
                    $"Waiting for start semaphore to upload part {partIndex} of {totalParts} for {fileId}...");
                using (await PriorityLock.EnterScopeAsync(1)) {
                }

                return await cellClient.Client.Upload_SaveBigFilePart(fileId, partIndex, totalParts,
                    buffer).WaitAsync(UploadTimeout);
            }, HandleFloodExceptionFunc, isValid: result => result);

            if (!partResult) {
                throw new Exception($"Failed to upload part {partIndex} for {fileId}.");
            }

            Logger.Trace($"Successfully uploaded part {partIndex} of {totalParts} for {fileId}...");
        } catch (Exception ex) {
            Logger.Error(ex, $"Failed to upload part {partIndex} of {totalParts} for {fileId}.");
            exceptions.Add(ex);
        } finally {
            UploadTaskSemaphore.Release();
        }
    }

    const string Failure420Key = "420";
    public static int Failure420Count { get; set; } = 1000;
    public static int FailureOtherCount { get; set; } = 20;

    public static readonly Func<Exception, Dictionary<string, int>?, Task<Dictionary<string, int>?>>
        HandleFloodExceptionFunc = HandleFloodException;

    public static async Task<Dictionary<string, int>?> HandleFloodException(Exception ex,
        Dictionary<string, int>? failures) {
        failures ??= new Dictionary<string, int>();
        switch (ex) {
            case RpcException {
                Code: 420
            } rpcException: {
                var count = failures.GetValueOrDefault(Failure420Key, 0) + 1;
                if (count > Failure420Count) {
                    Logger.Error(
                        $"Failed to avoid RpcException 420 after {Failure420Count} tries.");
                    throw ex;
                }

                failures[Failure420Key] = count;

                var nextRequest = DateTime.Now + TimeSpan.FromSeconds(rpcException.X);
                using (await PriorityLock.EnterScopeAsync(0)) {
                    var toSleep = nextRequest - DateTime.Now;

                    if (toSleep > TimeSpan.Zero) {
                        var message =
                            $"Sleep {toSleep.TotalSeconds:F2}s from now as was requested to sleep {rpcException.X}s ({count}).";
                        if (toSleep > TimeSpan.FromSeconds(30) || count % 100 == 0) {
                            Logger.Warn(ex, message);
                        } else {
                            Logger.Trace(ex, message);
                        }

                        await Task.Delay(toSleep);
                    }
                }

                return failures;
            }
            case WTException {
                Message: "You must connect to Telegram first"
            }:
                Logger.Warn(ex, $"Create new telegram client as requested.");
                sharedCellClient.Checked().Relogin();
                return failures;
            case TimeoutException or IOException or WTException or TaskCanceledException
                or RetryValidationException: {
                var failureKey = ex.GetType().ToString();
                var count = failures.GetValueOrDefault(failureKey, 0) + 1;
                if (count > FailureOtherCount) {
                    Logger.Error(
                        $"Failed to avoid unexpected exception after {FailureOtherCount} tries.");
                    throw ex;
                }

                failures[failureKey] = count;

                Logger.Warn(ex, $"Sleeping 30s for unexpected exception ({count})...");
                await Task.Delay(TimeSpan.FromSeconds(30));
                return failures;
            }
            default:
                throw ex;
        }
    }

    public static async Task<Dictionary<string, int>?> HandleMergeExceptions(Exception ex,
        Dictionary<string, int>? failures, Func<int, Task> getUploadBlockTask) {
        if (ex is RpcException {
                Code: 400,
                Message: "FILE_PART_X_MISSING",
                X: >= 0
            } rpcException) {
            var missingPart = rpcException.X;
            Logger.Warn(ex, $"Retry uploading block {missingPart}.");
            var task = getUploadBlockTask(missingPart);
            await task;
        } else {
            await HandleFloodException(ex, failures);
        }

        return failures;
    }

    public override Stream OpenRead(string path) {
        var cellClient = ObtainCellClient();
        var document = GetDocument(path);
        if (document == null) {
            throw new FileNotFoundException(
                $"{path} is not found in {this} because the document is not found in search.");
        }

        var fileSize = document.size;
        return new SeekableReadStream<DownloadState>(fileSize,
            (buffer, bufferOffset, offset, count, state) => Download(cellClient, buffer, path,
                bufferOffset, offset, count, state), new DownloadState(),
            disposer: ReturnCellClient);
    }

    const int DownloadBlockSize = 1 << 20; // 1 MiB

    class DownloadState {
        public readonly byte[] LastBlock = new byte[DownloadBlockSize];
        public long LastBlockStart = -1;
    }

    int Download(TelegramCellClient cellClient, byte[] buffer, string path, int bufferOffset,
        long offset, int count, DownloadState state) {
        // TODO: When will this happen?
        if (count < 0) {
            count = buffer.Length - bufferOffset;
        }

        var totalRead = 0;

        if (state.LastBlockStart >= 0 && offset >= state.LastBlockStart &&
            offset < state.LastBlockStart + DownloadBlockSize) {
            // Something can be read from lastBlock.
            var copySize = (int) Math.Min(count, state.LastBlockStart + DownloadBlockSize - offset);
            Array.Copy(state.LastBlock, offset - state.LastBlockStart, buffer, bufferOffset,
                copySize);
            count -= copySize;
            offset += copySize;
            bufferOffset += copySize;
            totalRead += copySize;
        }

        if (count == 0) {
            // Even though the next loop can handle this, return early so we don't mess lastBlock.
            return totalRead;
        }

        var downloadTasks = new List<Task>();

        // Workaround for expiration of document with a bit overhead.
        // This solution generally works if the next loop isn't taking too long. Performance wise,
        // this should serve for quite some versions. May need to revise if the next loop finishes
        // too quick.
        var location = GetDocument(path).Checked().ToFileLocation();
        Logger.Trace($"Getting {count} bytes from {offset} of {path}...");
        while (count > 0) {
            var effectiveReadCount =
                (int) Math.Min(count, DownloadBlockSize - offset % DownloadBlockSize);

            downloadTasks.Add(DownloadOneBlock(cellClient, location, count <= DownloadBlockSize,
                offset, effectiveReadCount, buffer, bufferOffset, state));

            count -= effectiveReadCount;
            offset += effectiveReadCount;
            bufferOffset += effectiveReadCount;
            totalRead += effectiveReadCount;
        }

        Task.WhenAll(downloadTasks).GetAwaiter().GetResult();

        return totalRead;
    }

    async Task DownloadOneBlock(TelegramCellClient cellClient, InputDocumentFileLocation location,
        bool isLastBlock, long offset, int effectiveReadCount, byte[] buffer, int bufferOffset,
        DownloadState downloadState) {
        var requestStart = offset.RoundDown(DownloadBlockSize);

        Logger.Trace($"To request {DownloadBlockSize} from {requestStart}.");

        Logger.Trace($"Waiting for semaphore to get block from {offset}...");
        await DownloadTaskSemaphore.WaitAsync();

        Logger.Trace($"Getting block from {offset}...");

        try {
            // From https://core.telegram.org/api/files#downloading-files
            // limit is at most 1 MiB and offset should align 1 MiB block boundary.
            var downloadResult = await Retry.Run(async () => {
                Logger.Trace($"Waiting for start semaphore to download from {offset}...");
                using (await PriorityLock.EnterScopeAsync(1)) {
                }

                return await cellClient.Client.Upload_GetFile(location, offset: requestStart,
                    limit: DownloadBlockSize, cdn_supported: true).WaitAsync(DownloadTimeout);
            }, HandleFloodExceptionFunc);

            if (downloadResult is not Upload_File uploadFile) {
                throw new Exception($"Response is not {nameof(Upload_File)}");
            }

            Array.Copy(uploadFile.bytes, offset - requestStart, buffer, bufferOffset,
                effectiveReadCount);

            // Keep the last block no matter if it's fully used or not as we normally will keep the
            // 512KB array there.
            if (isLastBlock) {
                downloadState.LastBlockStart = requestStart;
                uploadFile.bytes.CopyTo(downloadState.LastBlock, 0);
            }
        } finally {
            DownloadTaskSemaphore.Release();
        }
    }

    public override string Type => "tele";
    public override string Id => Cell.Checked().Id;

    Document? GetDocument(string path) {
        var message = GetMessage(path);

        return (message?.media as MessageMediaDocument)?.document as Document;
    }

    static readonly TimeSpan SearchDelay = TimeSpan.FromMinutes(30);

    Message? GetMessage(string path) {
        var cellClient = ObtainCellClient();

        try {
            var message = Retry
                .Run(
                    () => cellClient.Client.Messages_Search<InputMessagesFilterDocument>(
                        cellClient.Channel, path), HandleFloodExceptionFunc).GetAwaiter()
                .GetResult().Messages.Select(m => m as Message)
                .SingleOrDefault(m => m?.message == path);
            if (message != null) {
                return message;
            }

            var messages = Retry
                .Run(() => cellClient.Client.Messages_GetHistory(cellClient.Channel),
                    HandleFloodExceptionFunc).GetAwaiter().GetResult().Messages;
            while (messages?.Length > 0) {
                message = messages.Select(m => m as Message)
                    .SingleOrDefault(m => m?.message == path);
                if (message != null) {
                    return message;
                }

                if (messages[^1].Date < DateTime.UtcNow - SearchDelay) {
                    break;
                }

                var lastMessageId = messages[^1].ID;
                messages = Retry
                    .Run(
                        () => cellClient.Client.Messages_GetHistory(cellClient.Channel,
                            lastMessageId), HandleFloodExceptionFunc).GetAwaiter().GetResult()
                    .Messages;
            }

            return null;
        } finally {
            ReturnCellClient();
        }
    }

    public override void Dispose() {
        sharedCellClient?.Dispose();
        sharedCellClient = null;
    }
}
