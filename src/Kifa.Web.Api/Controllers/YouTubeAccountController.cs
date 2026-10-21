using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kifa.Service;
using Kifa.YouTube;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace Kifa.Web.Api.Controllers;

public class
    YouTubeAccountController : KifaDataController<YouTubeAccount,
    YouTubeAccountJsonServiceClient> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [HttpPost("$add_session")]
    public KifaApiActionResult AddSession([FromBody] YouTubeAccount.AddSessionRequest request)
        => Client.AddSession(request.AccountId, request.Cookies);

    [HttpPost("$obtain_session")]
    public async Task<KifaApiActionResult<YouTubeSession>> ObtainSession(
        [FromBody] YouTubeAccount.ObtainSessionRequest request,
        CancellationToken cancellationToken) {
        Logger.Trace($"Got request: obtain_session({request.ToJson()})");
        if (!Enum.IsDefined(request.Type)) {
            return new KifaActionResult<YouTubeSession> {
                Status = KifaActionStatus.BadRequest,
                Message = $"Invalid session type: {request.Type}."
            };
        }

        try {
            Logger.Debug($"Obtaining {request.Type} session for account {request.AccountId}...");
            var result = await Client.ObtainSessionAsync(request.AccountId, request.Type,
                request.SessionId, cancellationToken: cancellationToken);
            if (result.Status == KifaActionStatus.OK) {
                Logger.Debug(
                    $"Obtained {request.Type} session {result.Value?.Id} for account {request.AccountId}.");
            } else {
                Logger.Debug(
                    $"Failed to obtain {request.Type} session for account {request.AccountId}: {result.Status} - {result.Message}");
            }

            Logger.Trace($"Processed request: {result.Value?.ToJson()}");
            return result;
        } catch (OperationCanceledException) {
            Logger.Debug($"Request obtain_session({request.AccountId}) cancelled by client.");
            throw;
        }
    }

    [HttpPost("$renew_session")]
    public KifaApiActionResult RenewSession([FromBody] YouTubeAccount.RenewSessionRequest request)
        => Client.RenewSession(request.AccountId, request.SessionId);

    [HttpPost("$release_session")]
    public KifaApiActionResult
        ReleaseSession([FromBody] YouTubeAccount.ReleaseSessionRequest request)
        => Client.ReleaseSession(request.AccountId, request.SessionId, request.Cookies);

    [HttpPost("$update_session")]
    public KifaApiActionResult
        UpdateSession([FromBody] YouTubeAccount.UpdateSessionRequest request)
        => Client.UpdateSession(request.AccountId, request.SessionId, request.Cookies);
}

public partial class YouTubeAccountJsonServiceClient : KifaServiceJsonClient<YouTubeAccount>,
    YouTubeAccount.ServiceClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    KifaServiceJsonClient<YouTubeSession> GetSessionClient()
        => new() {
            DataFolder = DataFolder
        };

    static string NormalizeSessionId(string accountId, string sessionId)
        => sessionId.Contains('/') ? sessionId : $"{accountId}/{sessionId}";

    static string GetNextSessionId(string accountId, YouTubeAccount account) {
        var nextIndex = 0;
        foreach (var reservation in account.Sessions) {
            var id = reservation.Session.Id;
            var slashIndex = id.LastIndexOf('/');
            var indexPart = slashIndex >= 0 ? id[(slashIndex + 1)..] : id;
            if (int.TryParse(indexPart, out var parsed) && parsed >= nextIndex) {
                nextIndex = parsed + 1;
            }
        }

        return $"{accountId}/{nextIndex}";
    }

    public KifaActionResult AddSession(string accountId, string cookies) {
        using (AcquireLock(accountId)) {
            var account = Get(accountId).Checked();
            var sessionId = GetNextSessionId(accountId, account);
            var now = DateTimeOffset.UtcNow;
            var sessionResult = GetSessionClient().Set(new YouTubeSession {
                Id = sessionId,
                Cookies = cookies,
                Metadata = new DataMetadata {
                    Version = now,
                    LastRefreshed = now
                }
            });
            if (sessionResult.Status != KifaActionStatus.OK) {
                return sessionResult;
            }

            account.Sessions.Add(new YouTubeSessionReservation {
                Session = sessionId,
                Reserved = Date.Zero
            });
            var result = Update(account);
            NotifyWaiters(accountId);
            return result;
        }
    }

    static readonly TimeSpan LongLeaseDuration = TimeSpan.FromMinutes(10);
    static readonly TimeSpan QuickLeaseDuration = TimeSpan.FromMinutes(2);

    static TimeSpan GetLeaseDuration(YouTubeSessionType type)
        => type switch {
            YouTubeSessionType.Quick => QuickLeaseDuration,
            YouTubeSessionType.Download => LongLeaseDuration,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public KifaActionResult<YouTubeSession> ObtainSession(string accountId,
        YouTubeSessionType type, string? sessionId = null) {
        using (AcquireLock(accountId)) {
            var account = Get(accountId).Checked();

            var limit = YouTubeAccount.GetSessionLimit(type);
            var activeTypeCount = account.Sessions.Count(s =>
                s.Reserved > DateTimeOffset.UtcNow && s.Type == type);
            if (activeTypeCount >= limit) {
                return new KifaActionResult<YouTubeSession> {
                    Status = KifaActionStatus.Pending,
                    Message = $"All {limit} {type} sessions are reserved."
                };
            }

            var sessionClient = GetSessionClient();

            if (sessionId != null) {
                var fullSessionId = NormalizeSessionId(accountId, sessionId);
                var matchedReservation =
                    account.Sessions.FirstOrDefault(s => s.Session.Id == fullSessionId);
                if (matchedReservation != null &&
                    matchedReservation.Reserved <= DateTimeOffset.UtcNow) {
                    var matchedSession = sessionClient.Get(matchedReservation.Session.Id);
                    if (matchedSession != null) {
                        matchedReservation.Reserved =
                            DateTimeOffset.UtcNow + GetLeaseDuration(type);
                        matchedReservation.Type = type;

                        Update(account);
                        return matchedSession;
                    }
                }
            }

            var coldestReservation = account.Sessions
                .Where(s => s.Reserved <= DateTimeOffset.UtcNow)
                .MinBy(s => s.Reserved);
            if (coldestReservation == null) {
                if (account.Sessions.Count == 0) {
                    return new KifaActionResult<YouTubeSession> {
                        Status = KifaActionStatus.BadRequest,
                        Message = $"No valid sessions remaining for account {accountId}."
                    };
                }

                return new KifaActionResult<YouTubeSession> {
                    Status = KifaActionStatus.Pending,
                    Message = $"All {account.Sessions.Count} sessions are reserved."
                };
            }

            var coldestSession = sessionClient.Get(coldestReservation.Session.Id);
            if (coldestSession == null) {
                return new KifaActionResult<YouTubeSession> {
                    Status = KifaActionStatus.Error,
                    Message = $"Session {coldestReservation.Session.Id} is not found."
                };
            }

            coldestReservation.Reserved = DateTimeOffset.UtcNow + GetLeaseDuration(type);
            coldestReservation.Type = type;

            Update(account);
            return coldestSession;
        }
    }

    public KifaActionResult RenewSession(string accountId, string sessionId) {
        using (AcquireLock(accountId)) {
            var account = Get(accountId).Checked();
            var fullSessionId = NormalizeSessionId(accountId, sessionId);
            var reservation = account.Sessions.FirstOrDefault(s => s.Session.Id == fullSessionId);
            if (reservation == null || reservation.Reserved <= DateTimeOffset.UtcNow) {
                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Session {sessionId} is not found or has expired."
                };
            }

            if (reservation.Type == null) {
                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Session {sessionId} has no session type assigned."
                };
            }

            reservation.Reserved = DateTimeOffset.UtcNow + GetLeaseDuration(reservation.Type.Value);
            Update(account);

            NotifyWaiters(accountId);

            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Session renewed to {reservation.Reserved}"
            };
        }
    }

    public KifaActionResult ReleaseSession(string accountId, string sessionId,
        string? cookies = null) {
        using (AcquireLock(accountId)) {
            var account = Get(accountId).Checked();
            var fullSessionId = NormalizeSessionId(accountId, sessionId);
            var reservation = account.Sessions.FirstOrDefault(s => s.Session.Id == fullSessionId);
            if (reservation == null) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Warning,
                    Message = $"Session {sessionId} is not found."
                };
            }

            if (cookies != null) {
                var sessionResult = SaveSessionCookies(fullSessionId, cookies);
                if (sessionResult.Status != KifaActionStatus.OK) {
                    return sessionResult;
                }
            }

            // Set as now so that it will be least likely to be reserved again. So that we can
            // rotate sessions.
            reservation.Reserved = DateTimeOffset.UtcNow;
            reservation.Type = null;
            Update(account);

            NotifyWaiters(accountId);

            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Session {sessionId} is released."
            };
        }
    }

    public KifaActionResult UpdateSession(string accountId, string sessionId, string cookies) {
        using (AcquireLock(accountId)) {
            var account = Get(accountId).Checked();
            var fullSessionId = NormalizeSessionId(accountId, sessionId);
            var reservation = account.Sessions.FirstOrDefault(s => s.Session.Id == fullSessionId);
            if (reservation == null) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Error,
                    Message = $"Session {sessionId} is not found."
                };
            }

            var sessionResult = SaveSessionCookies(fullSessionId, cookies);
            if (sessionResult.Status != KifaActionStatus.OK) {
                return sessionResult;
            }

            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Session {sessionId} is updated."
            };
        }
    }

    KifaActionResult SaveSessionCookies(string fullSessionId, string cookies) {
        var sessionClient = GetSessionClient();
        var session = sessionClient.Get(fullSessionId) ?? new YouTubeSession {
            Id = fullSessionId
        };
        var now = DateTimeOffset.UtcNow;
        session.Metadata ??= new DataMetadata();
        if (session.Cookies != cookies) {
            session.Cookies = cookies;
            session.Metadata.Version = now;
        }

        session.Metadata.LastRefreshed = now;
        return sessionClient.Update(session);
    }
}
