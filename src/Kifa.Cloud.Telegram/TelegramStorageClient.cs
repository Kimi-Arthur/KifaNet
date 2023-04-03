using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Kifa.IO;
using Kifa.IO.StorageClients;
using NLog;
using TL;
using WTelegram;

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

    Client? client;
    public Client Client => client ??= GetClient();

    InputPeer? channel;

    public InputPeer Channel
        => channel ??= Client.Messages_GetAllChats().Result.chats[long.Parse(Cell.ChannelId)]
            .Checked();

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

        var result = Client.Checked().DeleteMessages(Channel, message.id).Result;
        if (result.pts_count != 1) {
            Logger.Debug($"Delete of {path} is not successful, but is ignored.");
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

        var size = stream.Length;
        var buffer = new byte[BlockSize];

        // size should be at most 1 << 31.
        var totalParts = (int) (size - 1) / BlockSize + 1;
        var fileId = Random.Shared.NextInt64();

        for (var i = 0; (long) i * BlockSize < size; ++i) {
            var readLength = stream.Read(buffer, 0, BlockSize);

            Logger.Trace($"Uploading part {i} of {totalParts} for {path}...");
            var partResult = Client.Upload_SaveBigFilePart(fileId, i, totalParts,
                readLength == BlockSize
                    ? buffer
                    : new ArraySegment<byte>(buffer, 0, readLength).ToArray()).Result;
            if (!partResult) {
                throw new Exception($"Failed to upload part {i} for {path}.");
            }
        }

        var finalResult = Client.SendMediaAsync(Channel, path, new InputFileBig {
            id = fileId,
            parts = totalParts,
            name = path.Split("/")[^1]
        }).Result;

        if (finalResult?.message != path) {
            throw new Exception(
                $"Failed to upload {path} in the finalization step: {finalResult}.");
        }
    }

    public override Stream OpenRead(string path) {
        var document = GetDocument(path).Checked();
        var fileSize = document.size;
        return new SeekableReadStream(fileSize,
            (buffer, bufferOffset, offset, count) => Download(buffer, path,
                bufferOffset, offset, count));
    }

    const int DownloadBlockSize = 1 << 20; // 1 MiB
    byte[]? lastBlock;
    long lastBlockStart = -1;

    int Download(byte[] buffer, string path, int bufferOffset, long offset, int count) {
        // TODO: When will this happen?
        if (count < 0) {
            count = buffer.Length - bufferOffset;
        }

        lastBlock ??= new byte[DownloadBlockSize];

        var totalRead = 0;

        if (lastBlockStart >= 0 && offset >= lastBlockStart &&
            offset < lastBlockStart + DownloadBlockSize) {
            // Something can be read from lastBlock.
            var copySize = (int) Math.Min(count, lastBlockStart + DownloadBlockSize - offset);
            Array.Copy(lastBlock, offset - lastBlockStart, buffer, bufferOffset, copySize);
            count -= copySize;
            offset += copySize;
            bufferOffset += copySize;
            totalRead += copySize;
        }

        if (count == 0) {
            // Even though the next loop can handle this, return early so we don't mess lastBlock.
            return totalRead;
        }

        // From https://core.telegram.org/api/files#downloading-files
        // limit is at most 1 MiB and offset should align 1 MiB block boundary.
        Upload_File? downloadResult = null;

        // Workaround for expiration of document with a bit overhead.
        // This solution generally works if the next loop isn't taking too long. Performance wise,
        // this should serve for quite some versions. May need to revise if the next loop finishes
        // too quick.
        var location = GetDocument(path).Checked().ToFileLocation();
        while (count > 0) {
            var requestStart = offset.RoundDown(DownloadBlockSize);
            lastBlockStart = requestStart;
            var requestCount = DownloadBlockSize;
            var effectiveReadCound = (int) Math.Min(count, BlockSize - offset % BlockSize);
            // var requestCount = (int) Math.Min(offset + count - requestStart, DownloadBlockSize);

            Logger.Trace(
                $"To request {requestCount} from {requestStart} for final target {count} bytes from {offset}.");

            downloadResult =
                (Client.Upload_GetFile(location, offset: requestStart, limit: DownloadBlockSize)
                    .Result as Upload_File).Checked();

            Array.Copy(downloadResult.bytes, offset - requestStart, buffer, bufferOffset,
                effectiveReadCound);
            count -= effectiveReadCound;
            offset += effectiveReadCound;
            bufferOffset += effectiveReadCound;
            totalRead += effectiveReadCound;
        }

        // Keep the last block no matter if it's fully used or not as we normally will keep the
        // 512KB array there.
        downloadResult.Checked().bytes.CopyTo(lastBlock, 0);

        return totalRead;
    }

    public override string Type => "tele";
    public override string Id => Cell.Checked().Id;

    Document? GetDocument(string path) {
        var message = GetMessage(path);

        return (message?.media as MessageMediaDocument)?.document as Document;
    }

    static readonly TimeSpan SearchDelay = TimeSpan.FromMinutes(30);

    public Message? GetMessage(string path) {
        var message = Client.Messages_Search<InputMessagesFilterDocument>(Channel, path).Result
            .Messages.Select(m => m as Message).SingleOrDefault(m => m?.message == path);
        if (message != null) {
            return message;
        }

        var messages = client.Messages_GetHistory(Channel).Result.Messages;
        while (messages?.Length > 0) {
            message = messages.Select(m => m as Message).SingleOrDefault(m => m?.message == path);
            if (message != null) {
                return message;
            }

            if (messages[^1].Date < DateTime.UtcNow - SearchDelay) {
                break;
            }

            messages = client.Messages_GetHistory(Channel, messages[^1].ID).Result.Messages;
        }

        return null;
    }

    static readonly ConcurrentDictionary<string, Client> AllClients = new();

    static Logger? wTelegramLogger;

    Client GetClient() {
        // Race condition should be OK here. Calling twice the clause shouldn't have visible
        // caveats.
        if (wTelegramLogger == null) {
            wTelegramLogger = LogManager.GetLogger("WTelegram");
            
            // Their Debug message is too noisy.
            Helpers.Log = (level, message)
                => wTelegramLogger.Log(LogLevel.FromOrdinal(level == 1 ? 0 : level), message);
        }

        return AllClients.GetOrAdd(CellId, (_, tele) => {
            var account = tele.Cell.Account.Data.Checked();
            var client = new Client(account.ApiId, account.ApiHash,
                $"{SessionsFolder}/{account.Id}.session");

            var result = client.Login(account.Phone).Result;
            if (result != null) {
                throw new DriveNotFoundException(
                    $"Telegram drive {tele.Cell.Id} is not accessible. Requesting {result}.");
            }

            return client;
        }, this);
    }
}
