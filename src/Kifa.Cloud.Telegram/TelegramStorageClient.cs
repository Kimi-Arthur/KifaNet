using System;
using System.IO;
using System.Linq;
using Kifa.IO;
using NLog;
using TL;
using WTelegram;

namespace Kifa.Cloud.Telegram;

public class TelegramStorageClient : StorageClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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

    public override Stream OpenRead(string path) {
        var document = GetDocument(path).Checked();
        var fileSize = document.size;
        return new SeekableReadStream(fileSize,
            (buffer, bufferOffset, offset, count) => Download(buffer, document.ToFileLocation(),
                bufferOffset, offset, count));
    }

    int Download(byte[] buffer, InputDocumentFileLocation location, int bufferOffset, long offset,
        int count) {
        if (count < 0) {
            count = buffer.Length - bufferOffset;
        }

        var downloadResult =
            (Client.Upload_GetFile(location, offset: offset, limit: count).Result as Upload_File)
            .Checked();

        downloadResult.bytes.CopyTo(buffer, bufferOffset);
        return downloadResult.bytes.Length;
    }

    const int BlockSize = 1 << 19; // 512KB

    public override void Write(string path, Stream stream) {
        // Due to https://github.com/wiz0u/WTelegramClient/issues/136,
        // we skip this sanity check for now.
        // if (Exists(path)) {
        //     return;
        // }

        var size = stream.Length;
        var buffer = new byte[BlockSize];

        // size should be at most 1 << 31.
        var totalParts = (int) (size - 1) / BlockSize + 1;
        var fileId = Random.Shared.NextInt64();

        for (var i = 0; (long) i * BlockSize < size; ++i) {
            var readLength = stream.Read(buffer, 0, BlockSize);

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

    public override string Type => "tele";
    public override string Id => Cell.Checked().Id;

    Document? GetDocument(string path) {
        var message = GetMessage(path);

        return (message?.media as MessageMediaDocument)?.document as Document;
    }

    public Message? GetMessage(string path)
        => Client.Messages_Search<InputMessagesFilterDocument>(Channel, path).Result.Messages
            .Select(m => m as Message).SingleOrDefault(m => m?.message == path);

    Client GetClient() {
        var account = Cell.Account.Data.Checked();
        var client = new Client(account.ApiId, account.ApiHash,
            $"{SessionsFolder}/{account.Id}.session");

        var result = client.Login(account.Phone).Result;
        if (result != null) {
            throw new DriveNotFoundException(
                $"Telegram drive {Cell.Id} is not accessible. Requesting {result}.");
        }

        return client;
    }
}
