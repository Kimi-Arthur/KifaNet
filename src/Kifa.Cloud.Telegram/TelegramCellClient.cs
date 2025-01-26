using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kifa.Service;
using NLog;
using TL;
using WTelegram;

namespace Kifa.Cloud.Telegram;

public class TelegramCellClient : IDisposable {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    TelegramAccount Account { get; set; }
    string ChannelId { get; set; }
    TelegramSession Session { get; set; }

    public Client Client { get; set; }
    public InputPeer Channel { get; set; }

    // TODO: More captured way to control this.
    public bool Reserved = true;
    bool disposed;

    static Logger? wTelegramLogger;

    public TelegramCellClient(TelegramAccount account, string channelId, TelegramSession session) {
        Logger.Trace($"Create client with session id {session.Id}.");
        Account = account;
        ChannelId = channelId;
        Session = session;

        // Race condition should be OK here. Calling twice the clause shouldn't have visible
        // caveats.
        if (wTelegramLogger == null) {
            ThreadPool.SetMinThreads(100, 100);
            wTelegramLogger = LogManager.GetLogger("WTelegram");

            // Always use Trace level as WTelegram logs can be noisy.
            Helpers.Log = (_, message) => wTelegramLogger.Log(LogLevel.FromOrdinal(0), message);
        }

        KeepSessionReserved(Session.Id);

        CreateClient();
    }

    void CreateClient() {
        var sessionStream = new MemoryStream();
        sessionStream.Write(Session.Data);
        sessionStream.Seek(0, SeekOrigin.Begin);

        Client = new Client(Account.ConfigProvider, sessionStream);
        Client.FloodRetryThreshold = 0;

        try {
            var result = Retry.Run(() => Client.Login(Account.Phone),
                TelegramStorageClient.HandleFloodExceptionFunc).GetAwaiter().GetResult();
            if (result != null) {
                throw new DriveNotFoundException(
                    $"Telegram drive {Account.Id} is not accessible. Requesting {result}.");
            }

            Channel = Retry
                .Run(() => Client.Messages_GetAllChats(),
                    TelegramStorageClient.HandleFloodExceptionFunc).GetAwaiter().GetResult()
                .chats[long.Parse(ChannelId)].Checked();
        } catch (WTException) {
            Dispose();
            throw;
        }
    }

    // TODO: Find a better way to keep the session.
    async Task KeepSessionReserved(int sessionId) {
        while (true) {
            await Task.Delay(TimeSpan.FromMinutes(5));

            if (disposed) {
                break;
            }

            if (Reserved) {
                Logger.LogResult(TelegramAccount.Client.RenewSession(Account.Id, sessionId),
                    $"reserving session {sessionId}", defaultLevel: LogLevel.Trace);
            }
        }
    }

    public void Relogin() {
        Client.Dispose();
        CreateClient();
    }

    public void Release() {
        Reserved = false;
        TelegramAccount.Client.ReleaseSession(Account.Id, Session.Id);
    }

    public void Dispose() {
        disposed = true;
        Logger.Trace($"Client with session id {Session.Id} is disposed.");
        Client.Dispose();
    }
}
