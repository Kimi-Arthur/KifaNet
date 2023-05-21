using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TL;
using WTelegram;

namespace Kifa.Cloud.Telegram;

public class TelegramCellClient : IDisposable {
    string AccountId { get; set; }
    int SessionId { get; set; }
    public Client Client { get; set; }
    public InputPeer Channel { get; set; }

    // TODO: More captured way to control this.
    public bool Reserved = true;

    static Logger? wTelegramLogger;

    public TelegramCellClient(TelegramAccount account, string channelId, TelegramSession session) {
        // Race condition should be OK here. Calling twice the clause shouldn't have visible
        // caveats.
        if (wTelegramLogger == null) {
            ThreadPool.SetMinThreads(100, 100);
            wTelegramLogger = LogManager.GetLogger("WTelegram");

            Helpers.Log = (level, message)
                => wTelegramLogger.Log(LogLevel.FromOrdinal(level < 3 ? 0 : level), message);
        }

        AccountId = account.Id;
        SessionId = session.Id;
        KeepSessionReserved(SessionId);

        var sessionStream = new MemoryStream();
        sessionStream.Write(session.Data);
        sessionStream.Seek(0, SeekOrigin.Begin);
        Client = new Client(account.ConfigProvider, sessionStream);

        try {
            var result = Retry.Run(() => Client.Login(account.Phone).GetAwaiter().GetResult(),
                TelegramStorageClient.HandleFloodException);
            if (result != null) {
                throw new DriveNotFoundException(
                    $"Telegram drive {account.Id} is not accessible. Requesting {result}.");
            }

            Channel = Retry
                .Run(() => Client.Messages_GetAllChats().GetAwaiter().GetResult(),
                    TelegramStorageClient.HandleFloodException).chats[long.Parse(channelId)]
                .Checked();
        } catch (WTException) {
            Dispose();
            throw;
        }
    }

    // TODO: Find a better way to keep the session.
    async Task KeepSessionReserved(int sessionId) {
        while (true) {
            await Task.Delay(TimeSpan.FromMinutes(5));

            if (!Reserved ||
                !TelegramAccount.Client.RenewSession(AccountId, sessionId).IsAcceptable) {
                break;
            }
        }
    }

    public void Release() {
        TelegramAccount.Client.ReleaseSession(AccountId, SessionId);
        Reserved = false;
    }

    public void Dispose() {
        Client.Dispose();
    }
}
