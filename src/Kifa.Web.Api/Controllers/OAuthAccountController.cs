using System.Net.Http;
using Kifa.Cloud.OAuth;
using Kifa.Service;
using Kifa.Web.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NLog;

namespace Kifa.Web.Api.Controllers;

public abstract class
    OAuthAccountController<TAccount> : KifaDataController<TAccount, OAuthAccountClient<TAccount>>
    where TAccount : OAuthAccount, WithModelId<TAccount>, new() {
    static readonly HttpClient HttpClient = new();
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static readonly KifaServiceJsonClient<TAccount> ServiceClient = new();

    public override ActionResult<TAccount?> Get(string id, bool refresh = false,
        [FromQuery] KifaDataOptions? options = null) {
        var result = base.Get(id, refresh, options);
        return result.Value != null ? result : AccountAdd(id);
    }

    public RedirectResult AccountAdd(string id)
        => Redirect(new TAccount().GetAuthUrl(this.ForAction(nameof(AccountRedirect)), id));

    [HttpGet("$redirect")]
    public IActionResult AccountRedirect([FromQuery] string code, [FromQuery] string state) {
        var response = HttpClient.Call(new TAccount().GetTokenRequest(code: code,
            redirectUrl: this.ForAction(nameof(AccountRedirect)))).Checked();

        var account = new TAccount {
            Id = state,
            AccessToken = response.AccessToken.Checked(),
            RefreshToken = response.RefreshToken.Checked(),
            Scope = response.Scope.Checked()
        };

        return account.FillUserInfo().And(() => ServiceClient.Set(account)).And(Redirect(
            this.ForAction(nameof(Get), new RouteValueDictionary {
                { "id", state }
            })));
    }
}

public class OAuthAccountClient<TAccount> : KifaServiceJsonClient<TAccount>
    where TAccount : OAuthAccount, WithModelId<TAccount>, new() {
}
