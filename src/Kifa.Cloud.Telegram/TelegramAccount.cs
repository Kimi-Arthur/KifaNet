using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kifa.Service;
using WTelegram;

namespace Kifa.Cloud.Telegram;

public class TelegramAccount : DataModel, WithModelId<TelegramAccount> {
    public static string ModelId => "telegram/accounts";

    #region Clients

    public static ServiceClient Client { get; set; } = new RestServiceClient();

    public interface ServiceClient : KifaServiceClient<TelegramAccount> {
        KifaActionResult AddSession(string accountId, byte[] sessionData);
        public KifaActionResult<TelegramSession> ObtainSession(string accountId,
            TelegramSessionType type, int? sessionId = null);
        public Task<KifaActionResult<TelegramSession>> ObtainSessionAsync(string accountId,
            TelegramSessionType type, int? sessionId = null,
            CancellationToken cancellationToken = default);
        public KifaActionResult RenewSession(string accountId, int sessionId);
        public KifaActionResult ReleaseSession(string accountId, int sessionId,
            int? floodWaitSeconds = null);
        public KifaActionResult UpdateSession(string accountId, int sessionId, byte[] sessionData);
    }

    public class AddSessionRequest {
        public required string AccountId { get; set; }
        public required byte[] SessionData { get; set; }
    }

    public class ObtainSessionRequest {
        public required string AccountId { get; set; }
        public required TelegramSessionType Type { get; set; }
        public int? SessionId { get; set; }
    }

    public class RenewSessionRequest {
        public required string AccountId { get; set; }
        public required int SessionId { get; set; }
    }

    public class ReleaseSessionRequest {
        public required string AccountId { get; set; }
        public required int SessionId { get; set; }
        public int? FloodWaitSeconds { get; set; }
    }

    public class UpdateSessionRequest {
        public required string AccountId { get; set; }
        public required int SessionId { get; set; }
        public required byte[] SessionData { get; set; }
    }

    public class RestServiceClient : KifaServiceRestClient<TelegramAccount>, ServiceClient {
        public KifaActionResult AddSession(string accountId, byte[] sessionData)
            => Call("add_session", new AddSessionRequest {
                AccountId = accountId,
                SessionData = sessionData
            });

        public KifaActionResult<TelegramSession> ObtainSession(string accountId,
            TelegramSessionType type, int? sessionId = null)
            => Call<TelegramSession>("obtain_session", new ObtainSessionRequest {
                AccountId = accountId,
                Type = type,
                SessionId = sessionId
            }).Checked();

        public Task<KifaActionResult<TelegramSession>> ObtainSessionAsync(string accountId,
            TelegramSessionType type, int? sessionId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ObtainSession(accountId, type, sessionId));

        public KifaActionResult RenewSession(string accountId, int sessionId)
            => Call("renew_session", new RenewSessionRequest {
                AccountId = accountId,
                SessionId = sessionId
            });

        public KifaActionResult ReleaseSession(string accountId, int sessionId,
            int? floodWaitSeconds = null)
            => Call("release_session", new ReleaseSessionRequest {
                AccountId = accountId,
                SessionId = sessionId,
                FloodWaitSeconds = floodWaitSeconds
            });

        public KifaActionResult UpdateSession(string accountId, int sessionId, byte[] sessionData)
            => Call("update_session", new UpdateSessionRequest {
                AccountId = accountId,
                SessionId = sessionId,
                SessionData = sessionData
            });
    }

    #endregion

    public int ApiId {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public string ApiHash {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public string Phone {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public List<TelegramSession> Sessions { get; set; } = new();

    public static int MaxUploadSessions { get; set; } = 2;
    public static int MaxDownloadSessions { get; set; } = 2;
    public static int MaxQuickSessions { get; set; } = 5;

    public static int GetSessionLimit(TelegramSessionType type)
        => type switch {
            TelegramSessionType.Upload => MaxUploadSessions,
            TelegramSessionType.Download => MaxDownloadSessions,
            TelegramSessionType.Quick => MaxQuickSessions,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public string? ConfigProvider(string configKey)
        => configKey switch {
            "api_id" => ApiId.ToString(),
            "api_hash" => ApiHash,
            _ => null
        };

    static readonly TimeSpan RefreshInterval = TimeSpan.FromDays(1);

    public void RefreshIfNeeded(TelegramSession session) {
        if (DateTimeOffset.UtcNow < session.Refreshed + RefreshInterval) {
            return;
        }

        var sessionStream = new MemoryStream();
        sessionStream.Write(session.Data);
        sessionStream.Seek(0, SeekOrigin.Begin);
        using var client = new Client(ConfigProvider, sessionStream);

        var result = client.Checked().Login(Phone).GetAwaiter().GetResult();
        if (result != null) {
            throw new DriveNotFoundException(
                $"Telegram drive {Id} is not accessible. Requesting {result}.");
        }

        session.Data = sessionStream.ToByteArray();
        session.Refreshed = DateTimeOffset.UtcNow;
    }
}
