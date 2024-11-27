using System;
using System.Linq;
using Kifa.Cloud.Telegram;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace Kifa.Web.Api.Controllers;

public class
    TelegramAccountController : KifaDataController<TelegramAccount,
    TelegramAccountJsonServiceClient> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [HttpPost("$add_session")]
    public KifaApiActionResult AddSession([FromBody] TelegramAccount.AddSessionRequest request)
        => Client.AddSession(request.AccountId, request.SessionData);

    [HttpPost("$obtain_session")]
    public KifaApiActionResult<TelegramSession> ObtainSession(
        [FromBody] TelegramAccount.ObtainSessionRequest request) {
        Logger.Trace($"Got request: obtain_session({request.ToJson()})");
        var result = Client.ObtainSession(request.AccountId, request.SessionId);
        Logger.Trace($"Processed request: {result.Response.ToJson()}");

        return result;
    }

    [HttpPost("$renew_session")]
    public KifaApiActionResult RenewSession([FromBody] TelegramAccount.RenewSessionRequest request)
        => Client.RenewSession(request.AccountId, request.SessionId);

    [HttpPost("$release_session")]
    public KifaApiActionResult
        ReleaseSession([FromBody] TelegramAccount.ReleaseSessionRequest request)
        => Client.ReleaseSession(request.AccountId, request.SessionId);

    [HttpPost("$update_session")]
    public KifaApiActionResult
        UpdateSession([FromBody] TelegramAccount.UpdateSessionRequest request)
        => Client.UpdateSession(request.AccountId, request.SessionId, request.SessionData);
}

public class TelegramAccountJsonServiceClient : KifaServiceJsonClient<TelegramAccount>,
    TelegramAccount.ServiceClient {
    public KifaActionResult AddSession(string accountId, byte[] sessionData) {
        lock (GetLock(accountId)) {
            var account = Get(accountId).Checked();
            account.Sessions.Add(new TelegramSession {
                Id = Random.Shared.Next(),
                Data = sessionData,
                Reserved = Date.Zero
            });
            return Update(account);
        }
    }

    static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(10);

    public KifaActionResult<TelegramSession> ObtainSession(string accountId, int? sessionId) {
        lock (GetLock(accountId)) {
            var account = Get(accountId).Checked();
            if (sessionId != null) {
                var matchedSession = account.Sessions.FirstOrDefault(s => s.Id == sessionId);
                // Session should already expire/be returned, otherwise RenewSession should be used.
                if (matchedSession != null && DateTimeOffset.UtcNow >= matchedSession.Reserved) {
                    account.RefreshIfNeeded(matchedSession);
                    matchedSession.Reserved = DateTimeOffset.UtcNow + LeaseDuration;

                    Update(account);
                    return matchedSession;
                }
            }

            var coldestSession = account.Sessions.MinBy(s => s.Reserved);
            if (!(coldestSession?.Reserved < DateTimeOffset.UtcNow)) {
                return new KifaActionResult<TelegramSession> {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"All {account.Sessions.Count} sessions are reserved."
                };
            }

            coldestSession.Reserved = DateTimeOffset.UtcNow + LeaseDuration;
            coldestSession.Id = Random.Shared.Next();
            account.RefreshIfNeeded(coldestSession);

            Update(account);
            return coldestSession;
        }
    }

    public KifaActionResult RenewSession(string accountId, int sessionId) {
        lock (GetLock(accountId)) {
            var account = Get(accountId).Checked();
            var session = account.Sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session == null) {
                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Session {sessionId} is not found."
                };
            }

            session.Reserved = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(10);
            Update(account);

            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Session renewed to {session.Reserved}"
            };
        }
    }

    public KifaActionResult ReleaseSession(string accountId, int sessionId) {
        lock (GetLock(accountId)) {
            var account = Get(accountId).Checked();
            var session = account.Sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session == null) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Warning,
                    Message = $"Session {sessionId} is not found."
                };
            }

            // Set as now so that it will be least likely to be reserved again. So that we can
            // rotate accounts.
            session.Reserved = DateTimeOffset.UtcNow;
            Update(account);

            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Session {sessionId} is released."
            };
        }
    }

    public KifaActionResult UpdateSession(string accountId, int sessionId, byte[] sessionData) {
        lock (GetLock(accountId)) {
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
