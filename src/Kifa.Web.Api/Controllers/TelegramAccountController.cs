using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kifa.Cloud.Telegram;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NLog;
using TL;

namespace Kifa.Web.Api.Controllers;

public class
    TelegramAccountController : KifaDataController<TelegramAccount,
    TelegramAccountJsonServiceClient> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [HttpPost("$add_session")]
    public KifaApiActionResult AddSession([FromBody] TelegramAccount.AddSessionRequest request)
        => Client.AddSession(request.AccountId, request.SessionData);

    [HttpPost("$obtain_session")]
    public async Task<KifaApiActionResult<TelegramSession>> ObtainSession(
        [FromBody] TelegramAccount.ObtainSessionRequest request,
        CancellationToken cancellationToken,
        [FromServices] IHostApplicationLifetime? hostApplicationLifetime = null) {
        Logger.Trace($"Got request: obtain_session({request.ToJson()})");
        if (!Enum.IsDefined(request.Type) || request.Type == TelegramSessionType.Refresh) {
            return new KifaActionResult<TelegramSession> {
                Status = KifaActionStatus.BadRequest,
                Message = $"Invalid session type: {request.Type}."
            };
        }

        try {
            Logger.Debug($"Obtaining {request.Type} session for account {request.AccountId}...");
            var result = await Client.ObtainSessionAsync(request.AccountId, request.Type,
                request.SessionId, cancellationToken: cancellationToken,
                stoppingToken: hostApplicationLifetime?.ApplicationStopping ?? default);
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
    public KifaApiActionResult RenewSession([FromBody] TelegramAccount.RenewSessionRequest request)
        => Client.RenewSession(request.AccountId, request.SessionId);

    [HttpPost("$release_session")]
    public KifaApiActionResult
        ReleaseSession([FromBody] TelegramAccount.ReleaseSessionRequest request)
        => Client.ReleaseSession(request.AccountId, request.SessionId, request.FloodWaitSeconds);

    [HttpPost("$update_session")]
    public KifaApiActionResult
        UpdateSession([FromBody] TelegramAccount.UpdateSessionRequest request)
        => Client.UpdateSession(request.AccountId, request.SessionId, request.SessionData);
}

public partial class TelegramAccountJsonServiceClient : KifaServiceJsonClient<TelegramAccount>,
    TelegramAccount.ServiceClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public KifaActionResult AddSession(string accountId, byte[] sessionData) {
        using (AcquireLock(accountId)) {
            var account = Get(accountId).Checked();
            account.Sessions.Add(new TelegramSession {
                Id = Random.Shared.Next(),
                Data = sessionData,
                Reserved = Date.Zero
            });
            var result = Update(account);
            NotifyWaiters(accountId);
            return result;
        }
    }

    static readonly TimeSpan LongLeaseDuration = TimeSpan.FromMinutes(10);
    static readonly TimeSpan QuickLeaseDuration = TimeSpan.FromMinutes(2);
    static readonly TimeSpan DefaultRefreshErrorDelay = TimeSpan.FromMinutes(2);

    static TimeSpan GetLeaseDuration(TelegramSessionType type)
        => type switch {
            TelegramSessionType.Quick => QuickLeaseDuration,
            TelegramSessionType.Upload or TelegramSessionType.Download => LongLeaseDuration,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public KifaActionResult<TelegramSession> ObtainSession(string accountId,
        TelegramSessionType type, int? sessionId = null) {
        using (AcquireLock(accountId)) {
            var account = Get(accountId).Checked();

            var limit = TelegramAccount.GetSessionLimit(type);
            var activeTypeCount = account.Sessions.Count(s =>
                s.Reserved > DateTimeOffset.UtcNow && s.Type == type);
            if (activeTypeCount >= limit) {
                return new KifaActionResult<TelegramSession> {
                    Status = KifaActionStatus.Pending,
                    Message = $"All {limit} {type} sessions are reserved."
                };
            }

            if (sessionId != null) {
                var matchedSession = account.Sessions.FirstOrDefault(s => s.Id == sessionId);
                // Session should already expire/be returned, otherwise RenewSession should be used.
                if (matchedSession != null && matchedSession.Reserved <= DateTimeOffset.UtcNow) {
                    if (TryLeaseSession(account, matchedSession, type, regenerateId: false,
                            out var leasedSession)) {
                        return leasedSession.Checked();
                    }
                }
            }

            while (account.Sessions.Count > 0) {
                var coldestSession = account.Sessions.Where(s => s.Reserved <= DateTimeOffset.UtcNow)
                    .MinBy(s => s.Reserved);
                if (coldestSession == null) {
                    return new KifaActionResult<TelegramSession> {
                        Status = KifaActionStatus.Pending,
                        Message = $"All {account.Sessions.Count} sessions are reserved."
                    };
                }

                if (TryLeaseSession(account, coldestSession, type, regenerateId: true,
                        out var leasedSession)) {
                    return leasedSession.Checked();
                }
            }

            return new KifaActionResult<TelegramSession> {
                Status = KifaActionStatus.BadRequest,
                Message = $"No valid sessions remaining for account {accountId}."
            };
        }
    }

    bool TryLeaseSession(TelegramAccount account, TelegramSession session,
        TelegramSessionType type, bool regenerateId, out TelegramSession? leasedSession) {
        leasedSession = null;
        try {
            account.RefreshIfNeeded(session);
            session.Reserved = DateTimeOffset.UtcNow + GetLeaseDuration(type);
            session.Type = type;
            if (regenerateId) {
                session.Id = Random.Shared.Next();
            }

            Update(account);
            leasedSession = session;
            return true;
        } catch (RpcException ex) when (ex.Code == 401 || ex.Message is "AUTH_KEY_UNREGISTERED" or "SESSION_REVOKED" or "SESSION_EXPIRED") {
            Logger.Error(ex,
                $"Dead session {session.Id} for account {account.Id}. Removing dead session.");
            account.Sessions.Remove(session);
            Update(account);
            return false;
        } catch (RpcException ex) when (ex.Code == 420 || ex.Message.StartsWith("FLOOD_WAIT_") || ex.X > 0) {
            var waitSeconds = ex.X > 0 ? ex.X + 1 : 60;
            Logger.Warn(ex,
                $"Flood wait of {waitSeconds}s refreshing session {session.Id} for account {account.Id}.");
            session.Reserved = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(waitSeconds);
            session.Type = TelegramSessionType.Refresh;
            Update(account);
            return false;
        } catch (Exception ex) {
            Logger.Warn(ex,
                $"Transient failure refreshing session {session.Id} for account {account.Id}.");
            session.Reserved = DateTimeOffset.UtcNow + DefaultRefreshErrorDelay;
            session.Type = TelegramSessionType.Refresh;
            Update(account);
            return false;
        }
    }

    public KifaActionResult RenewSession(string accountId, int sessionId) {
        using (AcquireLock(accountId)) {
            var account = Get(accountId).Checked();
            var session = account.Sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session == null || session.Reserved <= DateTimeOffset.UtcNow) {
                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Session {sessionId} is not found or has expired."
                };
            }

            if (session.Type == null || session.Type == TelegramSessionType.Refresh) {
                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Session {sessionId} has no session type assigned."
                };
            }

            session.Reserved = DateTimeOffset.UtcNow + GetLeaseDuration(session.Type.Value);
            Update(account);

            NotifyWaiters(accountId);

            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Session renewed to {session.Reserved}"
            };
        }
    }

    public KifaActionResult ReleaseSession(string accountId, int sessionId,
        int? floodWaitSeconds = null) {
        using (AcquireLock(accountId)) {
            var account = Get(accountId).Checked();
            var session = account.Sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session == null) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Warning,
                    Message = $"Session {sessionId} is not found."
                };
            }

            if (floodWaitSeconds is > 0) {
                session.Reserved =
                    DateTimeOffset.UtcNow + TimeSpan.FromSeconds(floodWaitSeconds.Value);
                session.Type = TelegramSessionType.Refresh;
            } else {
                // Set as now so that it will be least likely to be reserved again. So that we can
                // rotate accounts.
                session.Reserved = DateTimeOffset.UtcNow;
                session.Type = null;
            }

            Update(account);

            NotifyWaiters(accountId);

            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Session {sessionId} is released."
            };
        }
    }

    public KifaActionResult UpdateSession(string accountId, int sessionId, byte[] sessionData) {
        using (AcquireLock(accountId)) {
            var account = Get(accountId).Checked();
            var session = account.Sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session == null) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Error,
                    Message = $"Session {sessionId} is not found."
                };
            }

            session.Data = sessionData;
            Update(account);

            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Session {sessionId} is updated."
            };
        }
    }
}
