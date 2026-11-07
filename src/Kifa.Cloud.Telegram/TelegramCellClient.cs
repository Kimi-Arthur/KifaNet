using System;
using System.Collections.Generic;
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
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    TelegramAccount Account { get; set; }
    string ChannelId { get; set; }
    public TelegramSession Session { get; private set; }
    public TelegramSessionType Type { get; }

    public Client Client { get; set; }
    public InputPeer Channel { get; set; }

    // TODO: More captured way to control this.
    public bool Reserved = true;
    bool disposed;
    bool clientReady;
    DateTime lastInitializedAt;

    static Logger? wTelegramLogger;

    static readonly object ActiveClientsLock = new();
    static readonly HashSet<TelegramCellClient> ActiveClients = [];

    static TelegramCellClient() {
        KifaShutdown.RegisterCleanup(ReleaseAll);
    }

    public static void ReleaseAll() {
        List<TelegramCellClient> clients;
        lock (ActiveClientsLock) {
            clients = [.. ActiveClients];
        }

        foreach (var client in clients) {
            try {
                client.Release();
            } catch (Exception ex) {
                Logger.Warn(ex, $"Failed to release client with session {client.Session.Id}.");
            }
        }
    }

    public TelegramCellClient(TelegramAccount account, string channelId, TelegramSession session,
        TelegramSessionType type) {
        Logger.Trace($"Create client with session id {session.Id}.");
        Account = account;
        ChannelId = channelId;
        Session = session;
        Type = type;

        lock (ActiveClientsLock) {
            ActiveClients.Add(this);
        }

        // Race condition should be OK here. Calling twice the clause shouldn't have visible
        // caveats.
        if (wTelegramLogger == null) {
            ThreadPool.SetMinThreads(100, 100);
            wTelegramLogger = LogManager.GetLogger("WTelegram");

            // Always use Trace level as WTelegram logs can be noisy.
            Helpers.Log = (_, message) => wTelegramLogger.Log(LogLevel.FromOrdinal(0), message);
        }

        _ = KeepSessionReserved(Session.Id, type);

        try {
            InitializeClient();
        } catch {
            Release();
            throw;
        }
    }

    void InitializeClient() {
        while (true) {
            try {
                CreateClient();
                return;
            } catch (Exception ex) when (TelegramAccount.IsInvalidSessionException(ex)) {
                var oldSessionId = Session.Id;
                Logger.Warn(ex,
                    $"Session {oldSessionId} for account {Account.Id} is invalid. Removing invalid session and obtaining a new one...");
                Reserved = false;
                try {
                    TelegramAccount.Client.ReleaseSession(Account.Id.Checked(), oldSessionId,
                        invalid: true);
                } catch (Exception releaseEx) {
                    Logger.Warn(releaseEx,
                        $"Failed to release invalid session {oldSessionId} for account {Account.Id}.");
                }

                Session = ObtainNewSession();
                Reserved = true;
                _ = KeepSessionReserved(Session.Id, Type);
            }
        }
    }

    void CreateClient() {
        var sessionStream = new MemoryStream();
        sessionStream.Write(Session.Data);
        sessionStream.Seek(0, SeekOrigin.Begin);

        Client = new Client(Account.ConfigProvider, sessionStream);
        Client.FloodRetryThreshold = 0;

        try {
            Retry.Run(
                () => Account.VerifySessionAsync(Client)
                    .WaitAsync(TelegramStorageClient.QuickTimeout),
                TelegramStorageClient.HandleFloodExceptionFunc).GetAwaiter().GetResult();

            Channel = Retry
                .Run(() => Client.Messages_GetAllChats().WaitAsync(TelegramStorageClient.QuickTimeout),
                    TelegramStorageClient.HandleFloodExceptionFunc).GetAwaiter().GetResult()
                .chats[long.Parse(ChannelId)].Checked();
            clientReady = true;
            lastInitializedAt = DateTime.UtcNow;
        } catch {
            clientReady = false;
            Client.Dispose();
            throw;
        }
    }

    static readonly TimeSpan LongKeepSessionInterval = TimeSpan.FromMinutes(5);
    static readonly TimeSpan QuickKeepSessionInterval = TimeSpan.FromMinutes(1);

    // TODO: Find a better way to keep the session.
    async Task KeepSessionReserved(int sessionId, TelegramSessionType type) {
        var interval = type switch {
            TelegramSessionType.Quick => QuickKeepSessionInterval,
            TelegramSessionType.Upload or TelegramSessionType.Download => LongKeepSessionInterval,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        while (true) {
            await Task.Delay(interval);

            if (disposed || !Reserved || Session.Id != sessionId) {
                break;
            }

            Logger.LogResult(TelegramAccount.Client.RenewSession(Account.Id, sessionId),
                $"reserving session {sessionId}", defaultLevel: LogLevel.Trace);
        }
    }

    public async Task<Client> GetClientForDC(int dcId)
        => dcId == 0 ? Client : await Client.GetClientForDC(-Math.Abs(dcId), true).WaitAsync(TelegramStorageClient.QuickTimeout);

    public void WaitForReady() {
        lock (reobtainLock) {
            if (disposed) {
                throw new ObjectDisposedException(nameof(TelegramCellClient));
            }

            if (!clientReady) {
                InitializeClient();
            }
        }
    }

    public void Relogin(Client? expectedClient = null) {
        lock (reobtainLock) {
            if (disposed) {
                throw new ObjectDisposedException(nameof(TelegramCellClient));
            }

            if (expectedClient != null && Client != expectedClient) {
                Logger.Debug("Client was already relogged in.");
                return;
            }

            clientReady = false;
            try {
                Client?.Dispose();
            } catch (Exception ex) {
                Logger.Trace(ex, "Exception disposing old client.");
            }

            InitializeClient();
        }
    }

    static readonly TimeSpan SessionWaitInterval = TimeSpan.FromSeconds(10);
    static readonly TimeSpan ReobtainCooldown = TimeSpan.FromSeconds(5);
    readonly object reobtainLock = new();

    public void ReobtainSession(int? floodWaitSeconds = null, int? expectedSessionId = null,
        bool invalid = false) {
        lock (reobtainLock) {
            if (disposed) {
                throw new ObjectDisposedException(nameof(TelegramCellClient));
            }

            if (expectedSessionId != null && Session.Id != expectedSessionId) {
                Logger.Debug(
                    $"Session was already reobtained (current: {Session.Id}, expected: {expectedSessionId}).");
                return;
            }

            if (!invalid && DateTime.UtcNow - lastInitializedAt < ReobtainCooldown) {
                Logger.Debug(
                    $"Session {Session.Id} was just initialized recently; skipping duplicate re-obtain.");
                return;
            }

            var oldSessionId = Session.Id;
            Logger.Warn(
                $"Re-obtaining session for account {Account.Id}. Releasing old session {oldSessionId} (flood wait: {floodWaitSeconds}s, invalid: {invalid})...");

            clientReady = false;
            Reserved = false;
            try {
                TelegramAccount.Client.ReleaseSession(Account.Id.Checked(), oldSessionId,
                    floodWaitSeconds, invalid);
            } catch (Exception ex) {
                Logger.Warn(ex,
                    $"Failed to release old session {oldSessionId} for account {Account.Id}.");
            }

            try {
                Client?.Dispose();
            } catch (Exception ex) {
                Logger.Trace(ex, "Exception disposing old client.");
            }

            Session = ObtainNewSession();
            Reserved = true;

            _ = KeepSessionReserved(Session.Id, Type);

            InitializeClient();
            Logger.Info(
                $"Successfully re-obtained new session {Session.Id} for account {Account.Id}.");
        }
    }

    TelegramSession ObtainNewSession() {
        while (true) {
            try {
                Logger.Debug($"Obtaining {Type} session for account {Account.Id}...");
                var response = TelegramAccount.Client.ObtainSession(Account.Id.Checked(), Type);
                if (response.Status != KifaActionStatus.OK || response.Value == null) {
                    throw new KifaActionFailedException(response);
                }

                Logger.Debug(
                    $"Obtained {Type} session {response.Value.Checked().Id} for account {Account.Id}.");
                return response.Value;
            } catch (KifaActionFailedException ex) {
                if (ex.ActionResult.IsRetryable) {
                    Logger.Warn(ex,
                        $"Failed to obtain {Type} session for account {Account.Id}: {ex.ActionResult.Message}. Retrying...");
                    if (ex.ActionResult.Status != KifaActionStatus.Pending) {
                        Thread.Sleep(SessionWaitInterval);
                    }

                    continue;
                }

                throw new InsufficientStorageException(
                    $"Failed to locate a session to use for account {Account.Id}: {ex.ActionResult.Message}");
            }
        }
    }

    public void Release() {
        if (Reserved) {
            Reserved = false;
            try {
                TelegramAccount.Client.ReleaseSession(Account.Id.Checked(), Session.Id);
            } catch (Exception ex) {
                Logger.Warn(ex, $"Failed to release session {Session.Id} for account {Account.Id}.");
            }
        }

        if (!disposed) {
            Dispose();
        }
    }

    public void Dispose() {
        disposed = true;
        lock (ActiveClientsLock) {
            ActiveClients.Remove(this);
        }

        Logger.Trace($"Client with session id {Session.Id} is disposed.");
        try {
            Client?.Dispose();
        } catch (Exception ex) {
            Logger.Trace(ex, "Exception disposing client.");
        }
    }
}
