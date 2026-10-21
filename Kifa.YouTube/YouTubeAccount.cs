using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kifa.Service;

namespace Kifa.YouTube;

public class YouTubeSessionReservation {
    public required Link<YouTubeSession> Session { get; set; }

    public DateTimeOffset Reserved { get; set; } = Date.Zero;

    public YouTubeSessionType? Type { get; set; }
}

public class YouTubeAccount : DataModel, WithModelId<YouTubeAccount> {
    public static string ModelId => "youtube/accounts";

    #region Clients

    public static ServiceClient Client { get; set; } = new RestServiceClient();

    public interface ServiceClient : KifaServiceClient<YouTubeAccount> {
        KifaActionResult AddSession(string accountId, string cookies);
        public KifaActionResult<YouTubeSession> ObtainSession(string accountId,
            YouTubeSessionType type, string? sessionId = null);
        public Task<KifaActionResult<YouTubeSession>> ObtainSessionAsync(string accountId,
            YouTubeSessionType type, string? sessionId = null,
            CancellationToken cancellationToken = default);
        public KifaActionResult RenewSession(string accountId, string sessionId);
        public KifaActionResult ReleaseSession(string accountId, string sessionId,
            string? cookies = null);
        public KifaActionResult UpdateSession(string accountId, string sessionId, string cookies);
    }

    public class AddSessionRequest {
        public required string AccountId { get; set; }
        public required string Cookies { get; set; }
    }

    public class ObtainSessionRequest {
        public required string AccountId { get; set; }
        public required YouTubeSessionType Type { get; set; }
        public string? SessionId { get; set; }
    }

    public class RenewSessionRequest {
        public required string AccountId { get; set; }
        public required string SessionId { get; set; }
    }

    public class ReleaseSessionRequest {
        public required string AccountId { get; set; }
        public required string SessionId { get; set; }
        public string? Cookies { get; set; }
    }

    public class UpdateSessionRequest {
        public required string AccountId { get; set; }
        public required string SessionId { get; set; }
        public required string Cookies { get; set; }
    }

    public class RestServiceClient : KifaServiceRestClient<YouTubeAccount>, ServiceClient {
        public KifaActionResult AddSession(string accountId, string cookies)
            => Call("add_session", new AddSessionRequest {
                AccountId = accountId,
                Cookies = cookies
            });

        public KifaActionResult<YouTubeSession> ObtainSession(string accountId,
            YouTubeSessionType type, string? sessionId = null)
            => Call<YouTubeSession>("obtain_session", new ObtainSessionRequest {
                AccountId = accountId,
                Type = type,
                SessionId = sessionId
            }).Checked();

        public Task<KifaActionResult<YouTubeSession>> ObtainSessionAsync(string accountId,
            YouTubeSessionType type, string? sessionId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ObtainSession(accountId, type, sessionId));

        public KifaActionResult RenewSession(string accountId, string sessionId)
            => Call("renew_session", new RenewSessionRequest {
                AccountId = accountId,
                SessionId = sessionId
            });

        public KifaActionResult ReleaseSession(string accountId, string sessionId,
            string? cookies = null)
            => Call("release_session", new ReleaseSessionRequest {
                AccountId = accountId,
                SessionId = sessionId,
                Cookies = cookies
            });

        public KifaActionResult UpdateSession(string accountId, string sessionId, string cookies)
            => Call("update_session", new UpdateSessionRequest {
                AccountId = accountId,
                SessionId = sessionId,
                Cookies = cookies
            });
    }

    #endregion

    public string? Username { get; set; }
    public string? Password { get; set; }

    public List<YouTubeSessionReservation> Sessions { get; set; } = new();

    public static int MaxDownloadSessions { get; set; } = 2;
    public static int MaxQuickSessions { get; set; } = 5;

    public static int GetSessionLimit(YouTubeSessionType type)
        => type switch {
            YouTubeSessionType.Download => MaxDownloadSessions,
            YouTubeSessionType.Quick => MaxQuickSessions,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
