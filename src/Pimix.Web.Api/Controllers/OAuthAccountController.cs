using System.Net.Http;
using Kifa.Cloud.OAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Kifa.Service;

namespace Pimix.Web.Api.Controllers {
    public abstract class
        OAuthAccountController<TAccount> : KifaDataController<TAccount, KifaServiceJsonClient<TAccount>>
        where TAccount : OAuthAccount, new() {
        static readonly HttpClient HttpClient = new();

        static readonly KifaServiceJsonClient<TAccount> ServiceClient = new();

        public override Microsoft.AspNetCore.Mvc.ActionResult<TAccount> Get(string id, bool refresh = false) {
            var account = ServiceClient.Get(id);
            return account.Id != null ? base.Get(id, refresh) : AccountAdd(id);
        }

        public RedirectResult AccountAdd(string id) =>
            Redirect(new TAccount().GetAuthUrl(this.ForAction(nameof(AccountRedirect)), id));

        [HttpGet("$redirect")]
        public Microsoft.AspNetCore.Mvc.ActionResult<TAccount> AccountRedirect([FromQuery] string code, [FromQuery] string state) {
            var tokenUrl = new TAccount().GetTokenUrl(code, this.ForAction(nameof(AccountRedirect)));

            var response = HttpClient.PostAsync(tokenUrl, null).Result.GetJToken();
            var account = new TAccount {
                Id = state,
                AccessToken = (string) response["access_token"],
                RefreshToken = (string) response["refresh_token"],
                Scope = (string) response["scope"]
            };

            account.FillUserInfo();
            ServiceClient.Set(account);

            return Redirect(this.ForAction(nameof(Get), new RouteValueDictionary {{"id", state}}));
        }

        public override PimixActionResult Refresh(RefreshRequest request) {
            var account = ServiceClient.Get(request.Id);
            account.RefreshAccount();
            return KifaActionResult.FromAction(() => ServiceClient.Set(account));
        }
    }
}
