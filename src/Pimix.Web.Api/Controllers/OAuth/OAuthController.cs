using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using Kifa.Cloud.GooglePhotos;
using Kifa.Cloud.GooglePhotos.PhotosApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers.OAuth {
    [Route("api/" + GoogleAccount.ModelId)]
    public class OAuthController : KifaDataController<GoogleAccount, PimixServiceJsonClient<GoogleAccount>> {
        const string AuthUrlPattern =
            "https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={client_id}&redirect_uri={redirect_url}&prompt=consent&access_type=offline&scope={scope}&state={state}";

        const string GetTokenUrlPattern =
            "https://oauth2.googleapis.com/token?grant_type=authorization_code&code={code}&client_id={client_id}&client_secret={client_secret}&redirect_uri={redirect_url}";

        const string UserInfoUrlPattern = "https://www.googleapis.com/userinfo/v2/me?access_token={access_token}";

        const string RefreshTokenUrlPattern =
            "https://oauth2.googleapis.com/token?grant_type=refresh_token&refresh_token={refresh_token}&client_id={client_id}&client_secret={client_secret}";

        static readonly HttpClient HttpClient = new();

        static readonly PimixServiceJsonClient<GoogleAccount> ServiceClient = new();

        public override ActionResult<GoogleAccount> Get(string id, bool refresh = false) {
            var account = ServiceClient.Get(id);
            if (account.UserName != null) {
                return base.Get(id, refresh);
            }

            return AccountAdd(id);
        }

        public RedirectResult AccountAdd(string id) {
            var targetUrl = AuthUrlPattern.Format(new Dictionary<string, string> {
                {"client_id", GoogleCloudConfigs.ClientId},
                {"redirect_url", HttpUtility.UrlEncode(this.ForAction(nameof(AccountRedirect)))},
                {"scope", HttpUtility.UrlEncode(GoogleCloudConfigs.Scope)},
                {"state", id}
            });

            return Redirect(targetUrl);
        }

        [HttpGet("$redirect")]
        public ActionResult<GoogleAccount> AccountRedirect([FromQuery] string code, [FromQuery] string state) {
            var tokenUrl = GetTokenUrlPattern.Format(new Dictionary<string, string> {
                {"code", code},
                {"client_id", GoogleCloudConfigs.ClientId},
                {"client_secret", GoogleCloudConfigs.ClientSecret},
                {"redirect_url", HttpUtility.UrlEncode(this.ForAction(nameof(AccountRedirect)))}
            });

            var response = HttpClient.PostAsync(tokenUrl, null).Result.GetJToken();
            var account = new GoogleAccount {
                Id = state,
                AccessToken = (string) response["access_token"],
                RefreshToken = (string) response["refresh_token"],
                Scope = (string) response["scope"]
            };

            var userInfoUrl =
                UserInfoUrlPattern.Format(new Dictionary<string, string> {{"access_token", account.AccessToken}});
            var info = HttpClient.GetAsync(userInfoUrl).Result.GetJToken();
            account.UserName = (string) info["email"];
            account.UserId = (string) info["id"];
            ServiceClient.Set(account);
            return Redirect(this.ForAction(nameof(Get), new RouteValueDictionary {{"id", state}}));
        }

        public override PimixActionResult Refresh(RefreshRequest request) {
            var account = ServiceClient.Get(request.Id);
            var refreshTokenUrl = RefreshTokenUrlPattern.Format(new Dictionary<string, string> {
                {"client_id", GoogleCloudConfigs.ClientId},
                {"client_secret", GoogleCloudConfigs.ClientSecret},
                {"refresh_token", account.RefreshToken}
            });

            var response = HttpClient.PostAsync(refreshTokenUrl, null).Result.GetJToken();
            account.AccessToken = (string) response["access_token"];
            return RestActionResult.FromAction(() => ServiceClient.Set(account));
        }
    }
}