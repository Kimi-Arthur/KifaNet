using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Kifa.Service;
using NLog;
using WTelegram;

namespace Kifa.Cloud.Telegram;

public class TelegramAccount : DataModel, WithModelId<TelegramAccount> {
    public static string ModelId => "telegram/accounts";

    #region Clients

    public static ServiceClient Client { get; set; } = new RestServiceClient();

    public interface ServiceClient : KifaServiceClient<TelegramAccount> {
        KifaActionResult AddSession(string accountId, byte[] sessionData);
    }

    public class AddSessionRequest {
        public required string AccountId { get; set; }
        public required byte[] SessionData { get; set; }
    }

    public class RestServiceClient : KifaServiceRestClient<TelegramAccount>, ServiceClient {
        public KifaActionResult AddSession(string accountId, byte[] sessionData)
            => Call("add_session", new AddSessionRequest {
                AccountId = accountId,
                SessionData = sessionData
            });
    }

    #endregion

    #region public late long ApiId { get; set; }

    int? apiId;

    public int ApiId {
        get => Late.Get(apiId);
        set => Late.Set(ref apiId, value);
    }

    #endregion

    #region public late string ApiHash { get; set; }

    string? apiHash;

    public string ApiHash {
        get => Late.Get(apiHash);
        set => Late.Set(ref apiHash, value);
    }

    #endregion

    #region public late string Phone { get; set; }

    string? phone;

    public string Phone {
        get => Late.Get(phone);
        set => Late.Set(ref phone, value);
    }

    #endregion

    public byte[] Session { get; set; } = Array.Empty<byte>();

    public List<TelegramSession> Sessions { get; set; } = new();

    public TelegramSession? ObtainSession() {
        lock (Sessions) {
            var any = Sessions.MinBy(s => s.Reserved);
            if (any != null) {
                any.Reserved = DateTimeOffset.UtcNow + TimeSpan.FromHours(1);
            }

            return any;
        }
    }

    public bool RenewSession(string sessionId) {
        lock (Sessions) {
            var any = Sessions.FirstOrDefault(s => s.Id == sessionId);
            if (any != null) {
                any.Reserved = DateTimeOffset.UtcNow + TimeSpan.FromHours(1);
            }

            return any != null;
        }
    }

    public bool ReleaseSession(string sessionId) {
        lock (Sessions) {
            var any = Sessions.FirstOrDefault(s => s.Id == sessionId);
            if (any != null) {
                any.Reserved = Date.Zero;
            }

            return any != null;
        }
    }

    static Logger? wTelegramLogger;

    static readonly ConcurrentDictionary<string, Client> AllClients = new();

    public Client GetClient() {
        // Race condition should be OK here. Calling twice the clause shouldn't have visible
        // caveats.
        if (wTelegramLogger == null) {
            ThreadPool.SetMinThreads(100, 100);
            wTelegramLogger = LogManager.GetLogger("WTelegram");

            Helpers.Log = (level, message)
                => wTelegramLogger.Log(LogLevel.FromOrdinal(level < 3 ? 0 : level), message);
        }

        return AllClients.GetOrAdd(Id, CreateClient, this);
    }

    Client CreateClient(string _, TelegramAccount tele) {
        var sessionStream = new MemoryStream();
        sessionStream.Write(Session);
        sessionStream.Seek(0, SeekOrigin.Begin);
        var client = new Client(ConfigProvider, sessionStream);

        var result = Retry.Run(() => client.Login(Phone).GetAwaiter().GetResult(),
            TelegramStorageClient.HandleFloodException);
        if (result != null) {
            throw new DriveNotFoundException(
                $"Telegram drive {Id} is not accessible. Requesting {result}.");
        }

        return client;
    }

    public string? ConfigProvider(string configKey)
        => configKey switch {
            "api_id" => ApiId.ToString(),
            "api_hash" => ApiHash,
            _ => null
        };
}
