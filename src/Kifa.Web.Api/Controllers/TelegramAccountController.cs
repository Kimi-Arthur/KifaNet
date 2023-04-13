using System;
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
}

public class TelegramAccountJsonServiceClient : KifaServiceJsonClient<TelegramAccount>,
    TelegramAccount.ServiceClient {
    public KifaActionResult AddSession(string accountId, byte[] sessionData) {
        Console.WriteLine(
            $"Add session for account {accountId} with {sessionData.Length} bytes of data." + $"");
        var account = Get(accountId).Checked();
        account.Sessions.Add(new TelegramSession {
            Id = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff"),
            Data = sessionData,
            Reserved = Date.Zero
        });
        return Update(account);
    }
}
