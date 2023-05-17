using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Cloud.Telegram;

public class TelegramAccount : DataModel, WithModelId<TelegramAccount> {
    public static string ModelId => "telegram/accounts";

    #region Clients

    public static ServiceClient Client { get; set; } = new RestServiceClient();

    public interface ServiceClient : KifaServiceClient<TelegramAccount> {
        KifaActionResult AddSession(string accountId, byte[] sessionData);
        public KifaActionResult<TelegramSession> ObtainSession(string accountId);
        public KifaActionResult RenewSession(string accountId, int sessionId);
        public KifaActionResult ReleaseSession(string accountId, int sessionId);
        public KifaActionResult UpdateSession(string accountId, int sessionId, byte[] sessionData);
    }

    public class AddSessionRequest {
        public required string AccountId { get; set; }
        public required byte[] SessionData { get; set; }
    }

    public class ObtainSessionRequest {
        public required string AccountId { get; set; }
    }

    public class RenewSessionRequest {
        public required string AccountId { get; set; }
        public required int SessionId { get; set; }
    }

    public class ReleaseSessionRequest {
        public required string AccountId { get; set; }
        public required int SessionId { get; set; }
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

        public KifaActionResult<TelegramSession> ObtainSession(string accountId)
            => Call<TelegramSession>("obtain_session", new ObtainSessionRequest {
                AccountId = accountId
            });

        public KifaActionResult RenewSession(string accountId, int sessionId)
            => Call("renew_session", new RenewSessionRequest {
                AccountId = accountId,
                SessionId = sessionId
            });

        public KifaActionResult ReleaseSession(string accountId, int sessionId)
            => Call("release_session", new ReleaseSessionRequest {
                AccountId = accountId,
                SessionId = sessionId
            });

        public KifaActionResult UpdateSession(string accountId, int sessionId, byte[] sessionData)
            => Call("update_session", new UpdateSessionRequest {
                AccountId = accountId,
                SessionId = sessionId,
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

    public List<TelegramSession> Sessions { get; set; } = new();

    public string? ConfigProvider(string configKey)
        => configKey switch {
            "api_id" => ApiId.ToString(),
            "api_hash" => ApiHash,
            _ => null
        };
}
