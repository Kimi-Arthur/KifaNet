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
                Data = sessionData,
                Reserved = Date.Zero
            });
            return Update(account);
        }
    }

    public KifaActionResult<TelegramSession> ObtainSession(string accountId) {
        Console.WriteLine($"account id is {accountId}");
        lock (GetLock(accountId)) {
            var account = Get(accountId).Checked();
            var coldestSession = account.Sessions.MinBy(s => s.Reserved);
            if (coldestSession?.Reserved < DateTimeOffset.UtcNow) {
                coldestSession.Reserved = DateTimeOffset.UtcNow + TimeSpan.FromHours(1);
                Update(account);
                coldestSession.Id = account.Sessions.IndexOf(coldestSession);
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
            if (sessionId < 0 || sessionId >= account.Sessions.Count) {
                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message =
                        $"Session id {sessionId} is out of range [0, {account.Sessions.Count})"
                };
            }

            var newLease = DateTimeOffset.UtcNow + TimeSpan.FromHours(1);
            account.Sessions[sessionId].Reserved = newLease;
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
            if (sessionId < 0 || sessionId >= account.Sessions.Count) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Warning,
                    Message =
                        $"Session id {sessionId} is out of range [0, {account.Sessions.Count})"
                };
            }

            account.Sessions[sessionId].Reserved = Date.Zero;
            Update(account);

            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Session released."
            };
        }
    }
}
