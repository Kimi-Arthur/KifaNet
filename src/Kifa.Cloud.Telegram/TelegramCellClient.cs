using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kifa.IO;
using Kifa.Service;
using NLog;
using TL;
using WTelegram;

namespace Kifa.Cloud.Telegram;

public class TelegramCellClient : IDisposable {
    string? AccountId { get; set; }
    int SessionId { get; set; }
    public Client Client { get; set; }
    public InputPeer Channel { get; set; }

    static Logger? wTelegramLogger;

    public TelegramCellClient(TelegramAccount account, string channelId) {
        // Race condition should be OK here. Calling twice the clause shouldn't have visible
        // caveats.
        if (wTelegramLogger == null) {
            ThreadPool.SetMinThreads(100, 100);
            wTelegramLogger = LogManager.GetLogger("WTelegram");

            Helpers.Log = (level, message)
                => wTelegramLogger.Log(LogLevel.FromOrdinal(level < 3 ? 0 : level), message);
        }

        AccountId = account.Id;
        var response = TelegramAccount.Client.ObtainSession(AccountId);
        if (response.Status != KifaActionStatus.OK) {
            throw new InsufficientStorageException(
                $"Failed to locate a session to use: {response.Message}");
        }

        var session = response.Response.Checked();
        SessionId = session.Id;
        KeepSessionRefreshed(SessionId);

        var sessionStream = new MemoryStream();
        sessionStream.Write(session.Data);
        sessionStream.Seek(0, SeekOrigin.Begin);
        Client = new Client(account.ConfigProvider, sessionStream);

        Channel = Retry
            .Run(() => Client.Messages_GetAllChats().GetAwaiter().GetResult(),
                TelegramStorageClient.HandleFloodException).chats[long.Parse(channelId)].Checked();
    }

    // TODO: Find a better way to keep the session.
    async Task KeepSessionRefreshed(int sessionId) {
        while (true) {
            await Task.Delay(TimeSpan.FromMinutes(5));

            if (AccountId == null ||
                !TelegramAccount.Client.RenewSession(AccountId, sessionId).IsAcceptable) {
                break;
            }
        }
    }

    public void Dispose() {
        Client.Dispose();
        TelegramAccount.Client.ReleaseSession(AccountId.Checked(), SessionId);
        AccountId = null;
    }
}
