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

    public TelegramStorageCell? Cell { get; set; }

    public Client? Client { get; set; }

    public InputPeer? Channel { get; set; }

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

    public override Stream OpenRead(string path) => throw new NotImplementedException();

    const int BlockSize = 1 << 19; // 512KB

    public override void Write(string path, Stream stream) {
        EnsureLoggedIn();
        if (Client == null || Channel == null) {
            throw new Exception(
                $"Failed to upload {path}, due login issue. client: {Client}, channel: {Channel}");
        }

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

    Message? GetMessage(string path) {
        var searchResults = Client.Messages_Search<InputMessagesFilterDocument>(Channel, path)
            .Result.Messages.Select(m => m as Message).ExceptNull().Where(m => m.message == path)
            .ToList();
        if (searchResults.Count != 1) {
            if (searchResults.Count == 0) {
                return null;
            }

            throw new Exception($"{searchResults.Count} files found for {path}");
        }

        return searchResults.First();
    }

    public void EnsureLoggedIn() {
        Cell ??= TelegramStorageCell.Client.Get(CellId)!;
        var account = Cell.Account.Data.Checked();
        Client ??= new Client(account.ApiId, account.ApiHash,
            $"{SessionsFolder}/{account.Id}.session");

        var result = Client.Login(account.Phone).Result;
        if (result != null) {
            throw new DriveNotFoundException(
                $"Telegram drive {Cell.Id} is not accessible. Requesting {result}.");
        }

        Channel ??= Client.Messages_GetAllChats().Result.chats[long.Parse(Cell.ChannelId)];
    }
}
