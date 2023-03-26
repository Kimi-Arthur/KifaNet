using System;
using System.IO;
using Kifa.IO;
using TL;
using WTelegram;

namespace Kifa.Cloud.Telegram;

public class TelegramStorageClient : StorageClient {
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
        return 0;
    }

    public override void Delete(string path) {
        throw new NotImplementedException();
    }

    public override void Touch(string path) {
        throw new NotImplementedException();
    }

    public override Stream OpenRead(string path) => throw new NotImplementedException();

    public override void Write(string path, Stream stream) {
        throw new NotImplementedException();
    }

    public override string Type { get; }
    public override string Id { get; }

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
