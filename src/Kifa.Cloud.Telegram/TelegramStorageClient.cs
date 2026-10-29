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

    class CellClientEntry {
        public TelegramCellClient? Client { get; set; }
        public int Holders { get; set; }
        public TaskCompletionSource<TelegramCellClient>? InitTask { get; set; }
    }

    static readonly object CellClientLock = new();
    static readonly Dictionary<(string CellId, TelegramSessionType Type), CellClientEntry> CellClients = new();

    int? preferredSessionId;

    TelegramCellClient ObtainCellClient(TelegramSessionType type) {
        var key = (CellId, type);
        Task<TelegramCellClient>? waitTask = null;
        CellClientEntry? entry = null;
        TaskCompletionSource<TelegramCellClient>? tcs = null;

        lock (CellClientLock) {
            if (CellClients.TryGetValue(key, out var existing)) {
                existing.Holders++;
                if (existing.Client != null) {
                    preferredSessionId = existing.Client.Session.Id;
                    return existing.Client;
                }

                waitTask = existing.InitTask.Checked().Task;
            } else {
                tcs = new TaskCompletionSource<TelegramCellClient>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                entry = new CellClientEntry {
                    Holders = 1,
                    InitTask = tcs
                };
                CellClients[key] = entry;
            }
        }

        if (waitTask != null) {
            try {
                var c = waitTask.GetAwaiter().GetResult();
                preferredSessionId = c.Session.Id;
                return c;
            } catch {
                ReturnCellClient(type);
                throw;
            }
        }

        TelegramCellClient client;
        try {
            client = Cell.CreateClient(type, preferredSessionId);
            preferredSessionId = client.Session.Id;
        } catch (Exception ex) {
            lock (CellClientLock) {
                CellClients.Remove(key);
            }

            tcs.Checked().SetException(ex);
            throw;
        }

        TelegramCellClient? clientToRelease = null;
        lock (CellClientLock) {
            if (entry.Checked().Holders <= 0) {
                CellClients.Remove(key);
                clientToRelease = client;
            } else {
                entry.Checked().Client = client;
            }
        }

        tcs.Checked().SetResult(client);
        clientToRelease?.Release();
        if (clientToRelease != null) {
            throw new Exception("Session client was released because holders reached 0.");
        }

        return client;
    }

    void ReturnCellClient(TelegramSessionType type) {
        TelegramCellClient? clientToRelease = null;
        lock (CellClientLock) {
            var key = (CellId, type);
            if (CellClients.TryGetValue(key, out var entry)) {
                if (entry.Client != null) {
                    preferredSessionId = entry.Client.Session.Id;
                }

                entry.Holders--;
                if (entry.Holders <= 0) {
                    clientToRelease = entry.Client;
                    CellClients.Remove(key);
                }
            }
        }

        clientToRelease?.Release();
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
        var cellClient = ObtainCellClient(TelegramSessionType.Quick);
        try {
            var document = GetDocument(cellClient, path);
            if (document == null) {
                throw new FileNotFoundException();
            }

            return document.size;
        } finally {
            ReturnCellClient(TelegramSessionType.Quick);
        }
    }

    public override void Delete(string path) {
        var cellClient = ObtainCellClient(TelegramSessionType.Quick);

        try {
            var message = GetMessage(cellClient, path);
            if (message == null) {
                Logger.Debug($"File {path} is not found.");
                return;
            }

            var result = Retry
                .Run(() => cellClient.Client.DeleteMessages(cellClient.Channel, message.id),
                    GetFloodHandler(cellClient)).GetAwaiter().GetResult();
            if (result.pts_count != 1) {
                Logger.Debug($"Delete of {path} is not successful, but is ignored.");
            }
        } finally {
            ReturnCellClient(TelegramSessionType.Quick);
        }
    }

    public override void Touch(string path) {
        throw new NotImplementedException();
    }

    const int BlockSize = 1 << 19; // 512 KiB
    const long QuickSessionThreshold = 1024; // 1 KiB

    public override void Write(string path, Stream stream) {
        if (Exists(path)) {
            return;
        }

        var size = stream.Length;
        var sessionType = size <= QuickSessionThreshold
            ? TelegramSessionType.Quick
            : TelegramSessionType.Upload;

        var cellClient = ObtainCellClient(sessionType);

        try {
            // This is to ensure we don't simultaneously read the input when uploading to avoid conflict.
            var uploadInputStreamSemaphore = new SemaphoreSlim(1);

            // size should be at most 1 << 31.
            var totalParts = (int) (size - 1) / BlockSize + 1;
            var fileId = Random.Shared.NextInt64();
            Logger.Debug($"Uploading {path} with temp file id {fileId}...");

            var exceptions = new ConcurrentBag<Exception>();
            using var cts = new CancellationTokenSource();
            var tasks = new Task[totalParts];

            Task GetUploadBlockTask(int i)
                => UploadOneBlock(cellClient, fileId, totalParts, i, stream, i * BlockSize,
                    (int) Math.Min(size - i * BlockSize, BlockSize), exceptions,
                    uploadInputStreamSemaphore, cts);

            for (var i = 0; (long) i * BlockSize < size; ++i) {
                tasks[i] = GetUploadBlockTask(i);
            }

            try {
                Task.WhenAll(tasks).GetAwaiter().GetResult();
            } catch {
                // Exceptions collected in exceptions bag.
            }

            if (exceptions.TryPeek(out var ex)) {
                throw ex;
            }

            if (Exists(path)) {
                throw new FileCorruptedException(
                    $"Another program may have uploaded the file {path} in cell {this}. Skip creating the file.");
            }

            var finalResult = Retry.Run(() => cellClient.Client.SendMediaAsync(cellClient.Channel,
                        path, new InputFileBig {
                            id = fileId,
                            parts = totalParts,
                            name = path.Split("/")[^1]
                        }).WaitAsync(MergeTimeout),
                    new Func<Exception, Dictionary<string, int>?, Task<Dictionary<string, int>?>>((
                            exception, failures)
                        => HandleMergeExceptions(exception, failures, GetUploadBlockTask, cellClient)))
                .GetAwaiter()
                .GetResult();

            if (finalResult.message != path) {
                throw new Exception(
                    $"Failed to upload {path} in the finalization step: {finalResult}.");
            }
        } finally {
            ReturnCellClient(sessionType);
        }
    }

    public static double PacingInterval { get; set; } = 0.05;
    static DateTime lastRequestTime = DateTime.MinValue;

    public static async Task WaitPacingAsync() {
        using (await PriorityLock.EnterScopeAsync(1)) {
            var now = DateTime.UtcNow;
            var toSleep = lastRequestTime + TimeSpan.FromSeconds(PacingInterval) - now;
            if (toSleep > TimeSpan.Zero) {
                await Task.Delay(toSleep);
            }

            lastRequestTime = DateTime.UtcNow;
        }
    }

    async Task UploadOneBlock(TelegramCellClient cellClient, long fileId, int totalParts,
        int partIndex, Stream stream, long fromPosition, int length,
        ConcurrentBag<Exception> exceptions, SemaphoreSlim uploadInputStreamSemaphore,
        CancellationTokenSource cts) {
        if (cts.IsCancellationRequested || !exceptions.IsEmpty) {
            return;
        }

        Logger.Trace(
            $"Waiting for uploadSemaphore to uploading part {partIndex} of {totalParts} for {fileId}...");
        try {
            await UploadTaskSemaphore.WaitAsync(cts.Token);
        } catch (OperationCanceledException) {
            return;
        }

        try {
            if (cts.IsCancellationRequested || !exceptions.IsEmpty) {
                return;
            }

            var buffer = new byte[length];

            try {
                await uploadInputStreamSemaphore.WaitAsync(cts.Token);
            } catch (OperationCanceledException) {
                return;
            }

            try {
                if (cts.IsCancellationRequested || !exceptions.IsEmpty) {
                    Logger.Trace("Other task already failed. Fail fast.");
                    return;
                }

                stream.Seek(fromPosition, SeekOrigin.Begin);
                var readLength = await stream.ReadAsync(buffer, cts.Token);
                if (readLength != length) {
                    throw new FileCorruptedException(
                        $"Unexpected read length {readLength}, expecting {length}");
                }
            } catch (OperationCanceledException) {
                return;
            } catch (Exception ex) {
                Logger.Error(ex,
                    $"Failed to get the input stream from {fromPosition} to upload part {partIndex} of {totalParts} for {fileId}.");
                exceptions.Add(ex);
                cts.Cancel();
                return;
            } finally {
                uploadInputStreamSemaphore.Release();
            }

            if (cts.IsCancellationRequested || !exceptions.IsEmpty) {
                Logger.Debug("Other task already failed. Fail fast.");
                return;
            }

            Logger.Trace($"Uploading part {partIndex} of {totalParts} for {fileId}...");
            var partResult = await Retry.Run(async () => {
                if (cts.IsCancellationRequested || !exceptions.IsEmpty) {
                    throw new OperationCanceledException();
                }

                Logger.Trace(
                    $"Waiting for start semaphore to upload part {partIndex} of {totalParts} for {fileId}...");
                await WaitPacingAsync();

                return await cellClient.Client.Upload_SaveBigFilePart(fileId, partIndex, totalParts,
                    buffer).WaitAsync(UploadTimeout);
            }, GetFloodHandler(cellClient), isValid: result => result);

            if (!partResult) {
                throw new Exception($"Failed to upload part {partIndex} for {fileId}.");
            }

            Logger.Trace($"Successfully uploaded part {partIndex} of {totalParts} for {fileId}...");
        } catch (OperationCanceledException) {
            // Ignore cancellation
        } catch (Exception ex) {
            Logger.Error(ex, $"Failed to upload part {partIndex} of {totalParts} for {fileId}.");
            exceptions.Add(ex);
            cts.Cancel();
        } finally {
            UploadTaskSemaphore.Release();
        }
    }

    const string Failure420Key = "420";
    const string Failure503Key = "503";
    public static int Failure420Count { get; set; } = 1000;
    public static int Failure503Count { get; set; } = 3;
    public static int FailureOtherCount { get; set; } = 20;
    public static TimeSpan FloodWarnThreshold { get; set; } = TimeSpan.FromSeconds(50);
    public static TimeSpan MaxAllowedFloodWait { get; set; } = TimeSpan.FromSeconds(30);
    public static int MaxFloodWaitRetries { get; set; } = 3;

    public static Func<Exception, Dictionary<string, int>?, Task<Dictionary<string, int>?>>
        GetFloodHandler(TelegramCellClient? cellClient = null)
        => (ex, failures) => HandleFloodException(ex, failures, cellClient);

    public static readonly Func<Exception, Dictionary<string, int>?, Task<Dictionary<string, int>?>>
        HandleFloodExceptionFunc = GetFloodHandler();

    public static async Task<Dictionary<string, int>?> HandleFloodException(Exception ex,
        Dictionary<string, int>? failures, TelegramCellClient? cellClient = null) {
        failures ??= new Dictionary<string, int>();
        switch (ex) {
            case RpcException {
                Code: 420
            } rpcException: {
                var count = failures.GetValueOrDefault(Failure420Key, 0) + 1;
                failures[Failure420Key] = count;

                var waitSeconds = rpcException.X;
                if (cellClient != null && (waitSeconds > MaxAllowedFloodWait.TotalSeconds ||
                                           count > MaxFloodWaitRetries)) {
                    Logger.Warn(ex,
                        $"Flood wait of {waitSeconds}s (try {count}) exceeded threshold (max wait: {MaxAllowedFloodWait.TotalSeconds}s, max tries: {MaxFloodWaitRetries}). Re-requesting session...");
                    cellClient.ReobtainSession(waitSeconds, cellClient.Session.Id);
                    failures.Remove(Failure420Key);
                    return failures;
                }

                if (count > Failure420Count) {
                    Logger.Error(
                        $"Failed to avoid RpcException 420 after {Failure420Count} tries.");
                    throw ex;
                }

                var extraWait = 1 + Random.Shared.NextDouble() * 15;
                var nextRequest = DateTime.Now + TimeSpan.FromSeconds(rpcException.X + extraWait);
                using (await PriorityLock.EnterScopeAsync(0)) {
                    var toSleep = nextRequest - DateTime.Now;

                    if (toSleep > TimeSpan.Zero) {
                        var message =
                            $"Sleep {toSleep.TotalSeconds:F2}s from now as was requested to sleep {rpcException.X}s ({count}).";
                        if (toSleep > FloodWarnThreshold || count % 100 == 0) {
                            Logger.Warn(ex, message);
                        } else {
                            Logger.Trace(ex, message);
                        }

                        await Task.Delay(toSleep);
                        lastRequestTime = DateTime.UtcNow;
                    }
                }

                return failures;
            }
            case RpcException {
                Code: -503 or 503
            } or RpcException {
                Message: "Timeout"
            }: {
                var count = failures.GetValueOrDefault(Failure503Key, 0) + 1;
                if (count > Failure503Count) {
                    Logger.Error(
                        $"Failed to avoid RpcException 503/Timeout after {Failure503Count} tries.");
                    throw ex;
                }

                failures[Failure503Key] = count;

                var sleepSeconds = 30 * Math.Pow(2, count - 1) + Random.Shared.NextDouble() * 10;
                Logger.Warn(ex, $"Sleeping {sleepSeconds:F2}s for server timeout ({count})...");
                await Task.Delay(TimeSpan.FromSeconds(sleepSeconds));
                return failures;
            }
            case RpcException {
                Code: 401 or 406
            } or RpcException {
                Message: "AUTH_KEY_DUPLICATED" or "AUTH_KEY_UNREGISTERED" or "SESSION_REVOKED"
                or "SESSION_EXPIRED"
            }: {
                Logger.Error(ex, $"Permanent Telegram authentication error: {ex.Message}. Failing immediately.");
                throw ex;
            }
            case WTException {
                Message: "You must connect to Telegram first"
            }:
                Logger.Warn(ex, "Telegram client disconnected. Resetting cell clients.");
                lock (CellClientLock) {
                    foreach (var entry in CellClients.Values) {
                        try {
                            entry.Client.Relogin();
                        } catch {
                            // Best effort relogin
                        }
                    }
                }
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
        Dictionary<string, int>? failures, Func<int, Task> getUploadBlockTask,
        TelegramCellClient? cellClient = null) {
        if (ex is RpcException {
            Code: 400,
            Message: "FILE_PART_X_MISSING",
            X: >= 0
        } rpcException) {
            var missingPart = rpcException.X;
            Logger.Warn(ex, $"Retry uploading block {missingPart}.");
            var task = getUploadBlockTask(missingPart);
            await task;
            return failures;
        }

        return await HandleFloodException(ex, failures, cellClient);
    }

    public override Stream OpenRead(string path) {
        var state = new DownloadState {
            SessionType = TelegramSessionType.Quick,
            CellClient = ObtainCellClient(TelegramSessionType.Quick)
        };

        try {
            var document = GetDocument(state.CellClient, path);
            if (document == null) {
                throw new FileNotFoundException(
                    $"{path} is not found in {this} because the document is not found in search.");
            }

            var fileSize = document.size;
            return new SeekableReadStream<DownloadState>(fileSize,
                (buffer, bufferOffset, offset, count, s) => Download(buffer, path,
                    bufferOffset, offset, count, s), state,
                disposer: () => {
                    lock (state) {
                        if (state.CellClient != null) {
                            ReturnCellClient(state.SessionType);
                            state.CellClient = null;
                        }
                    }
                });
        } catch {
            lock (state) {
                if (state.CellClient != null) {
                    ReturnCellClient(state.SessionType);
                    state.CellClient = null;
                }
            }

            throw;
        }
    }

    class DownloadState {
        public TelegramSessionType SessionType = TelegramSessionType.Quick;
        public TelegramCellClient? CellClient;
        public long BytesRead;
        public readonly byte[] LastBlock = new byte[BlockSize];
        public long LastBlockStart = -1;
        public Exception? LastException;
    }

    void UpgradeCellClient(DownloadState state, TelegramSessionType targetType) {
        lock (state) {
            if (state.SessionType == targetType || state.CellClient == null) {
                return;
            }

            Logger.Debug(
                $"Upgrading session from {state.SessionType} to {targetType} for cell {this}.");
            ReturnCellClient(state.SessionType);
            state.CellClient = null;
            state.SessionType = targetType;
            state.CellClient = ObtainCellClient(targetType);
        }
    }

    int Download(byte[] buffer, string path, int bufferOffset,
        long offset, int count, DownloadState state) {
        if (state.LastException != null) {
            throw state.LastException;
        }

        // TODO: When will this happen?
        if (count < 0) {
            count = buffer.Length - bufferOffset;
        }

        var requestedCount = count;

        TelegramCellClient? cellClient;
        lock (state) {
            if (state.SessionType == TelegramSessionType.Quick &&
                state.BytesRead + requestedCount > QuickSessionThreshold) {
                UpgradeCellClient(state, TelegramSessionType.Download);
            }

            cellClient = state.CellClient;
        }

        if (cellClient == null) {
            throw new ObjectDisposedException(nameof(SeekableReadStream));
        }

        var totalRead = 0;

        if (state.LastBlockStart >= 0 && offset >= state.LastBlockStart &&
            offset < state.LastBlockStart + BlockSize) {
            // Something can be read from lastBlock.
            var copySize = (int) Math.Min(count, state.LastBlockStart + BlockSize - offset);
            Array.Copy(state.LastBlock, offset - state.LastBlockStart, buffer, bufferOffset,
                copySize);
            count -= copySize;
            offset += copySize;
            bufferOffset += copySize;
            totalRead += copySize;
        }

        if (count == 0) {
            // Even though the next loop can handle this, return early so we don't mess lastBlock.
            lock (state) {
                state.BytesRead += totalRead;
            }

            return totalRead;
        }

        var downloadTasks = new List<Task>();
        var exceptions = new ConcurrentBag<Exception>();
        using var cts = new CancellationTokenSource();

        // Workaround for expiration of document with a bit overhead.
        // This solution generally works if the next loop isn't taking too long. Performance wise,
        // this should serve for quite some versions. May need to revise if the next loop finishes
        // too quick.
        var document = GetDocument(cellClient, path).Checked();
        var location = document.ToFileLocation();
        var dcId = document.dc_id;
        Logger.Trace($"Getting {count} bytes from {offset} of {path}...");
        while (count > 0) {
            var effectiveReadCount =
                (int) Math.Min(count, BlockSize - offset % BlockSize);

            downloadTasks.Add(DownloadOneBlock(cellClient, dcId, location, count <= BlockSize,
                offset, effectiveReadCount, buffer, bufferOffset, state, exceptions, cts));

            count -= effectiveReadCount;
            offset += effectiveReadCount;
            bufferOffset += effectiveReadCount;
            totalRead += effectiveReadCount;
        }

        try {
            Task.WhenAll(downloadTasks).GetAwaiter().GetResult();
        } catch {
            // Exceptions collected in exceptions bag.
        }

        if (exceptions.TryPeek(out var ex)) {
            state.LastException = ex;
            throw ex;
        }

        lock (state) {
            state.BytesRead += totalRead;
        }

        return totalRead;
    }

    async Task DownloadOneBlock(TelegramCellClient cellClient, int dcId,
        InputDocumentFileLocation location, bool isLastBlock, long offset, int effectiveReadCount,
        byte[] buffer, int bufferOffset, DownloadState downloadState,
        ConcurrentBag<Exception> exceptions, CancellationTokenSource cts) {
        if (cts.IsCancellationRequested || !exceptions.IsEmpty) {
            return;
        }

        var requestStart = offset.RoundDown(BlockSize);

        Logger.Trace($"To request {BlockSize} from {requestStart}.");

        Logger.Trace($"Waiting for semaphore to get block from {offset}...");
        try {
            await DownloadTaskSemaphore.WaitAsync(cts.Token);
        } catch (OperationCanceledException) {
            return;
        }

        Logger.Trace($"Getting block from {offset}...");

        try {
            if (cts.IsCancellationRequested || !exceptions.IsEmpty) {
                return;
            }

            var client = await cellClient.GetClientForDC(dcId);
            var downloadResult = await Retry.Run(async () => {
                if (cts.IsCancellationRequested || !exceptions.IsEmpty) {
                    throw new OperationCanceledException();
                }

                Logger.Trace($"Waiting for start semaphore to download from {offset}...");
                await WaitPacingAsync();

                var client = await cellClient.GetClientForDC(dcId);
                try {
                    return await client.Upload_GetFile(location, offset: requestStart,
                        limit: BlockSize).WaitAsync(DownloadTimeout);
                } catch (RpcException ex) when (ex.Code == 303 && ex.Message == "FILE_MIGRATE_X") {
                    client = await cellClient.GetClientForDC(ex.X);
                    return await client.Upload_GetFile(location, offset: requestStart,
                        limit: BlockSize).WaitAsync(DownloadTimeout);
                }
            }, GetFloodHandler(cellClient));

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
        } catch (OperationCanceledException) {
            // Ignore cancellation
        } catch (Exception ex) {
            Logger.Error(ex, $"Failed to download block from {offset}.");
            exceptions.Add(ex);
            cts.Cancel();
        } finally {
            DownloadTaskSemaphore.Release();
        }
    }

    public override string Type => "tele";
    public override string Id => Cell.Checked().Id;

    Document? GetDocument(TelegramCellClient cellClient, string path) {
        var message = GetMessage(cellClient, path);

        return (message?.media as MessageMediaDocument)?.document as Document;
    }

    static readonly TimeSpan SearchDelay = TimeSpan.FromMinutes(30);

    Message? GetMessage(TelegramCellClient cellClient, string path)
        => GetMessageBySearch(cellClient, path) ?? GetMessageByHistory(cellClient, path);

    Message? GetMessageByHistory(TelegramCellClient cellClient, string path) {
        var messages = Retry
            .Run(() => cellClient.Client.Messages_GetHistory(cellClient.Channel),
                GetFloodHandler(cellClient)).GetAwaiter().GetResult().Messages;
        while (messages?.Length > 0) {
            try {
                var message = messages.Select(m => m as Message)
                    .SingleOrDefault(m => m?.message == path);

                if (message != null) {
                    return message;
                }
            } catch (InvalidOperationException) {
                throw new FileCorruptedException(
                    $"Multiple entries matched for {path} by history in cell {this}");
            }

            if (messages[^1].Date < DateTime.UtcNow - SearchDelay) {
                break;
            }

            var lastMessageId = messages[^1].ID;
            messages = Retry
                .Run(() => cellClient.Client.Messages_GetHistory(cellClient.Channel, lastMessageId),
                    GetFloodHandler(cellClient)).GetAwaiter().GetResult().Messages;
        }

        return null;
    }

    Message? GetMessageBySearch(TelegramCellClient cellClient, string path) {
        try {
            return Retry
                .Run(
                    () => cellClient.Client.Messages_Search<InputMessagesFilterDocument>(
                        cellClient.Channel, path),
                    GetFloodHandler(cellClient)).GetAwaiter()
                .GetResult().Messages.Select(m => m as Message)
                .SingleOrDefault(m => m?.message == path);
        } catch (InvalidOperationException) {
            throw new FileCorruptedException(
                $"Multiple entries matched for {path} by search in cell {this}");
        }
    }

    public override void Dispose() {
    }
}
