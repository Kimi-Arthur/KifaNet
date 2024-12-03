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

    string AccountId { get; set; }
    int SessionId { get; set; }
    public Client Client { get; set; }
    public InputPeer Channel { get; set; }

    // TODO: More captured way to control this.
    public bool Reserved = true;
    bool disposed;

    static Logger? wTelegramLogger;

    public TelegramCellClient(TelegramAccount account, string channelId, TelegramSession session) {
        Logger.Trace($"Client with session id {session.Id} is created.");

        // Race condition should be OK here. Calling twice the clause shouldn't have visible
        // caveats.
        if (wTelegramLogger == null) {
            ThreadPool.SetMinThreads(100, 100);
            wTelegramLogger = LogManager.GetLogger("WTelegram");

            // Always use Trace level as WTelegram logs can be noisy.
            Helpers.Log = (_, message) => wTelegramLogger.Log(LogLevel.FromOrdinal(0), message);
        }

        AccountId = account.Id;
        SessionId = session.Id;
        KeepSessionReserved(SessionId);

        var sessionStream = new MemoryStream();
        sessionStream.Write(session.Data);
        sessionStream.Seek(0, SeekOrigin.Begin);
        Client = new Client(account.ConfigProvider, sessionStream);
        Client.FloodRetryThreshold = 0;

        try {
            var result = Retry.Run(() => Client.Login(account.Phone),
                TelegramStorageClient.HandleFloodExceptionFunc).GetAwaiter().GetResult();
            if (result != null) {
                throw new DriveNotFoundException(
                    $"Telegram drive {account.Id} is not accessible. Requesting {result}.");
            }

            Channel = Retry
                .Run(() => Client.Messages_GetAllChats(),
                    TelegramStorageClient.HandleFloodExceptionFunc).GetAwaiter().GetResult()
                .chats[long.Parse(channelId)].Checked();
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
                Logger.LogResult(TelegramAccount.Client.RenewSession(AccountId, sessionId),
                    $"reserving session {sessionId}", defaultLevel: LogLevel.Trace);
            }
        }
    }

    public void Release() {
        Reserved = false;
        TelegramAccount.Client.ReleaseSession(AccountId, SessionId);
    }

    public void Dispose() {
        disposed = true;
        Logger.Trace($"Client with session id {SessionId} is disposed.");
        Client.Dispose();
    }
}
