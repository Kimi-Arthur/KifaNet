using System;
using System.Linq;
using Kifa.Cloud.Telegram;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers;

public class
    TelegramAccountController : KifaDataController<TelegramAccount,
        TelegramAccountJsonServiceClient> {
    [HttpPost("$add_session")]
    public KifaApiActionResult AddSession([FromBody] TelegramAccount.AddSessionRequest request)
        => Client.AddSession(request.AccountId, request.SessionData);

    [HttpPost("$obtain_session")]
    public KifaApiActionResult<TelegramSession> ObtainSession(
        [FromBody] TelegramAccount.ObtainSessionRequest request)
        => Client.ObtainSession(request.AccountId);

    [HttpPost("$renew_session")]
    public KifaApiActionResult RenewSession([FromBody] TelegramAccount.RenewSessionRequest request)
        => Client.RenewSession(request.AccountId, request.SessionId);

    [HttpPost("$release_session")]
    public KifaApiActionResult
        ReleaseSession([FromBody] TelegramAccount.ReleaseSessionRequest request)
        => Client.ReleaseSession(request.AccountId, request.SessionId);
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

    public KifaActionResult<TelegramSession> ObtainSession(string accountId) {
        lock (GetLock(accountId)) {
            var account = Get(accountId).Checked();
            var coldestSession = account.Sessions.MinBy(s => s.Reserved);
            if (coldestSession?.Reserved < DateTimeOffset.UtcNow) {
                coldestSession.Reserved = DateTimeOffset.UtcNow + TimeSpan.FromHours(1);
                coldestSession.Id = Random.Shared.Next();
                Update(account);
                return coldestSession;
            }

            return new KifaActionResult<TelegramSession> {
                Status = KifaActionStatus.BadRequest,
                Message = account.Sessions.Count == 0
                    ? "No sessions available."
                    : $"All {account.Sessions.Count} sessions are reserved."
            };
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

            var newLease = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5);
            session.Reserved = newLease;
            Update(account);

            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Session renewed to {newLease}"
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

            session.Reserved = Date.Zero;
            session.Id = Random.Shared.Next();
            Update(account);

            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Session {sessionId} is released."
            };
        }
    }
}
