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

namespace Kifa.Cloud.Telegram;

public class TelegramStorageClient : StorageClient, CanCreateStorageClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const long ShardSize = 4000L * BlockSize; // 2 GB

    public static int DownloadThread { get; set; } = 8;
    public static int UploadThread { get; set; } = 8;

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
    TelegramCellClient? sharedCellClient;

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
                    HandleFloodException).GetAwaiter().GetResult();
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

            // This is to ensure we don't simulaneously read the input when uploading to avoid conflict.
            var uploadInputStreamSemaphore = new SemaphoreSlim(1);


            // size should be at most 1 << 31.
            var totalParts = (int) (size - 1) / BlockSize + 1;
            var fileId = Random.Shared.NextInt64();
            Logger.Debug($"Uploading {path} with temp file id {fileId}...");

            var exceptions = new ConcurrentBag<Exception>();
            var tasks = new Task[totalParts];
            for (var i = 0; (long) i * BlockSize < size; ++i) {
                tasks[i] = UploadOneBlock(cellClient, fileId, totalParts, i, stream, i * BlockSize,
                    (int) Math.Min(size - i * BlockSize, BlockSize), exceptions,
                    uploadInputStreamSemaphore);
            }

            Task.WhenAll(tasks).GetAwaiter().GetResult();
            if (exceptions.TryPeek(out var ex)) {
                throw ex;
            }

            var finalResult = Retry.Run(() => cellClient.Client.SendMediaAsync(cellClient.Channel,
                path, new InputFileBig {
                    id = fileId,
                    parts = totalParts,
                    name = path.Split("/")[^1]
                }), HandleFloodException).GetAwaiter().GetResult();

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
                    buffer);
            }, HandleFloodException, isValid: (result, _) => result);

            if (!partResult) {
                throw new Exception($"Failed to upload part {partIndex} for {fileId}.");
            }

            Logger.Trace($"Successfully uploaded part {partIndex} of {totalParts} for {fileId}...");
        } catch (Exception ex) {
            exceptions.Add(ex);
        } finally {
            UploadTaskSemaphore.Release();
        }
    }

    public static async Task HandleFloodException(Exception ex, int i) {
        switch (ex) {
            case IOException:
            case RpcException { Code: 500 }: {
                if (i >= 5) {
                    throw ex;
                }

                Logger.Warn(ex, $"Sleeping 30s for unexpected exception ({i})...");
                await Task.Delay(TimeSpan.FromSeconds(30));
                return;
            }
            case RpcException {
                Code: 420
            } when i >= 100:
                throw ex;
            case RpcException {
                Code: 420
            } rpcException:
                var nextRequest = DateTime.Now + TimeSpan.FromSeconds(rpcException.X);
                using (await PriorityLock.EnterScopeAsync(0)) {
                    var toSleep = nextRequest - DateTime.Now;

                    if (toSleep > TimeSpan.Zero) {
                        Logger.Warn(ex,
                            $"Sleep {toSleep.TotalSeconds:F2}s from now as requested to sleep {rpcException.X}s.");
                        await Task.Delay(toSleep);
                    }
                }

                return;
            default:
                throw ex;
        }
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
                    limit: DownloadBlockSize, cdn_supported: true);
            }, HandleFloodException);

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
                        cellClient.Channel, path), HandleFloodException).GetAwaiter().GetResult()
                .Messages.Select(m => m as Message).SingleOrDefault(m => m?.message == path);
            if (message != null) {
                return message;
            }

            var messages = Retry
                .Run(() => cellClient.Client.Messages_GetHistory(cellClient.Channel),
                    HandleFloodException).GetAwaiter().GetResult().Messages;
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
                            lastMessageId), HandleFloodException).GetAwaiter().GetResult().Messages;
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
