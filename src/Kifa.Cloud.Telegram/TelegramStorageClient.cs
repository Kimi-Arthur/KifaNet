using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kifa.IO;
using Kifa.IO.StorageClients;
using NLog;
using TL;

namespace Kifa.Cloud.Telegram;

public class TelegramStorageClient : StorageClient, CanCreateStorageClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const long ShardSize = 4000L * BlockSize; // 2 GB

    #region public late static string SessionsFolder { get; set; }

    static string? sessionsFolder;

    public static string SessionsFolder {
        get => Late.Get(sessionsFolder);
        set => Late.Set(ref sessionsFolder, value);
    }

    #endregion

    public required string CellId { get; init; }

    TelegramStorageCell? cell;
    public TelegramStorageCell Cell => cell ??= TelegramStorageCell.Client.Get(CellId).Checked();

    TelegramStorageClient() {
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
            return -1;
        }

        return document.size;
    }

    public override void Delete(string path) {
        var message = GetMessage(path);
        if (message == null) {
            Logger.Debug($"File {path} is not found.");
            return;
        }

        var result =
            Retry.Run(
                () => Cell.TelegramClient.DeleteMessages(Cell.Channel, message.id).GetAwaiter()
                    .GetResult(), HandleFloodException);
        if (result.pts_count != 1) {
            Logger.Debug($"Delete of {path} is not successful, but is ignored.");
        }
    }

    public override void Touch(string path) {
        throw new NotImplementedException();
    }

    const int BlockSize = 1 << 19; // 512 KiB

    public override void Write(string path, Stream stream) {
        Cell.ResetClient();
        if (Exists(path)) {
            return;
        }

        var size = stream.Length;

        // size should be at most 1 << 31.
        var totalParts = (int) (size - 1) / BlockSize + 1;
        var fileId = Random.Shared.NextInt64();
        Logger.Debug($"Uploading {path} with temp file id {fileId}...");

        var uploadSemaphore = new SemaphoreSlim(8);
        var readSemaphore = new SemaphoreSlim(1);
        var exceptions = new ConcurrentBag<Exception>();
        var tasks = new Task[totalParts];
        for (var i = 0; (long) i * BlockSize < size; ++i) {
            tasks[i] = UploadOneBlock(fileId, totalParts, i, stream, i * BlockSize,
                (int) Math.Min(size - i * BlockSize, BlockSize), readSemaphore, uploadSemaphore,
                exceptions);
        }

        Task.WhenAll(tasks).GetAwaiter().GetResult();
        if (exceptions.TryPeek(out var ex)) {
            throw ex;
        }

        var finalResult = Retry.Run(() => Cell.TelegramClient.SendMediaAsync(Cell.Channel, path,
            new InputFileBig {
                id = fileId,
                parts = totalParts,
                name = path.Split("/")[^1]
            }).GetAwaiter().GetResult(), HandleFloodException);

        if (finalResult.message != path) {
            throw new Exception(
                $"Failed to upload {path} in the finalization step: {finalResult}.");
        }
    }

    async Task UploadOneBlock(long fileId, int totalParts, int partIndex, Stream stream,
        long fromPosition, int length, SemaphoreSlim readSemaphore, SemaphoreSlim uploadSemaphore,
        ConcurrentBag<Exception> exceptions) {
        Logger.Trace(
            $"Waiting for uploadSemaphore to uploading part {partIndex} of {totalParts} for {fileId}...");
        await uploadSemaphore.WaitAsync();
        try {
            var buffer = new byte[length];

            await readSemaphore.WaitAsync();

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
                readSemaphore.Release();
            }

            if (!exceptions.IsEmpty) {
                Logger.Debug("Other task already failed. Fail fast.");
                return;
            }

            Logger.Trace($"Uploading part {partIndex} of {totalParts} for {fileId}...");
            var partResult = await Retry.Run(
                async () => await Cell.TelegramClient.Upload_SaveBigFilePart(fileId, partIndex,
                    totalParts, buffer), HandleFloodException);

            if (!partResult) {
                throw new Exception($"Failed to upload part {partIndex} for {fileId}.");
            }

            Logger.Trace($"Successfully uploaded part {partIndex} of {totalParts} for {fileId}...");
        } catch (Exception ex) {
            exceptions.Add(ex);
        } finally {
            uploadSemaphore.Release();
        }
    }

    public static void HandleFloodException(Exception ex, int i) {
        if (ex is not RpcException {
                Code: 420
            } rpcException) {
            throw ex;
        }

        if (i >= 10) {
            throw ex;
        }

        Logger.Warn(ex, $"Sleeping {rpcException.X} as requested by Telegram API ({i})...");
        Thread.Sleep(TimeSpan.FromSeconds(rpcException.X));
    }

    public override Stream OpenRead(string path) {
        Cell.ResetClient();
        var document = GetDocument(path);
        if (document == null) {
            throw new FileNotFoundException(
                $"{path} is not found in {this} because the document is not found in search.");
        }

        var fileSize = document.size;
        return new SeekableReadStream<DownloadState>(fileSize,
            (buffer, bufferOffset, offset, count, state) => Download(buffer, path,
                bufferOffset, offset, count, state), new DownloadState());
    }

    const int DownloadBlockSize = 1 << 20; // 1 MiB

    class DownloadState {
        public readonly byte[] LastBlock = new byte[DownloadBlockSize];
        public long LastBlockStart = -1;
    }

    int Download(byte[] buffer, string path, int bufferOffset, long offset, int count,
        DownloadState state) {
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

        var semaphore = new SemaphoreSlim(6);

        // Workaround for expiration of document with a bit overhead.
        // This solution generally works if the next loop isn't taking too long. Performance wise,
        // this should serve for quite some versions. May need to revise if the next loop finishes
        // too quick.
        var location = GetDocument(path).Checked().ToFileLocation();
        Logger.Trace($"Getting {count} bytes from {offset} of {path}...");
        while (count > 0) {
            var effectiveReadCount =
                (int) Math.Min(count, DownloadBlockSize - offset % DownloadBlockSize);

            downloadTasks.Add(DownloadOneBlock(location, count <= DownloadBlockSize, offset,
                effectiveReadCount, buffer, bufferOffset, state, semaphore));

            count -= effectiveReadCount;
            offset += effectiveReadCount;
            bufferOffset += effectiveReadCount;
            totalRead += effectiveReadCount;
        }

        Task.WhenAll(downloadTasks).GetAwaiter().GetResult();

        return totalRead;
    }

    async Task DownloadOneBlock(InputDocumentFileLocation location, bool isLastBlock, long offset,
        int effectiveReadCount, byte[] buffer, int bufferOffset, DownloadState downloadState,
        SemaphoreSlim semaphore) {
        var requestStart = offset.RoundDown(DownloadBlockSize);
        Logger.Trace($"To request {DownloadBlockSize} from {requestStart}.");

        Logger.Trace($"Waiting for semaphore to get block from {offset}...");
        await semaphore.WaitAsync();
        Logger.Trace($"Getting block from {offset}...");

        try {
            // From https://core.telegram.org/api/files#downloading-files
            // limit is at most 1 MiB and offset should align 1 MiB block boundary.
            var downloadResult = await Retry.Run(
                async () => await Cell.TelegramClient.Upload_GetFile(location, offset: requestStart,
                    limit: DownloadBlockSize), HandleFloodException);

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
            semaphore.Release();
        }
    }

    public override string Type => "tele";
    public override string Id => Cell.Checked().Id;

    Document? GetDocument(string path) {
        var message = GetMessage(path);

        return (message?.media as MessageMediaDocument)?.document as Document;
    }

    static readonly TimeSpan SearchDelay = TimeSpan.FromMinutes(30);

    public Message? GetMessage(string path) {
        var message = Retry
            .Run(
                () => Cell.TelegramClient
                    .Messages_Search<InputMessagesFilterDocument>(Cell.Channel, path).GetAwaiter()
                    .GetResult(), HandleFloodException).Messages.Select(m => m as Message)
            .SingleOrDefault(m => m?.message == path);
        if (message != null) {
            return message;
        }

        var messages = Retry
            .Run(
                () => Cell.TelegramClient.Messages_GetHistory(Cell.Channel).GetAwaiter()
                    .GetResult(), HandleFloodException).Messages;
        while (messages?.Length > 0) {
            message = messages.Select(m => m as Message).SingleOrDefault(m => m?.message == path);
            if (message != null) {
                return message;
            }

            if (messages[^1].Date < DateTime.UtcNow - SearchDelay) {
                break;
            }

            var lastMessageId = messages[^1].ID;
            messages = Retry
                .Run(
                    () => Cell.TelegramClient.Messages_GetHistory(Cell.Channel, lastMessageId)
                        .GetAwaiter().GetResult(), HandleFloodException).Messages;
        }

        return null;
    }
}
