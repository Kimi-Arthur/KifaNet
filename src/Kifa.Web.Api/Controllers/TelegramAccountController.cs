using System;
using Kifa.Cloud.Telegram;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers;

public class
    TelegramAccountController : KifaDataController<TelegramAccount,
        TelegramAccountJsonServiceClient> {
    [HttpPost("$add_session")]
    public KifaApiActionResult AddSession(string accountId, byte[] sessionData)
        => Client.AddSession(accountId, sessionData);
}

public class TelegramAccountJsonServiceClient : KifaServiceJsonClient<TelegramAccount>,
    TelegramAccount.ServiceClient {
    public KifaActionResult AddSession(string accountId, byte[] sessionData) {
        var account = Get(accountId).Checked();
        account.Sessions.Add(new TelegramSession {
            Id = DateTimeOffset.UtcNow.ToString(),
            Data = sessionData,
            Reserved = Date.Zero
        });
        return Update(account);
    }
}
