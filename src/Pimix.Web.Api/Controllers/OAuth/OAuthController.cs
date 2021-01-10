using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using Kifa.Cloud.GooglePhotos;
using Kifa.Cloud.GooglePhotos.PhotosApi;
using Microsoft.AspNetCore.Mvc;

namespace Pimix.Web.Api.Controllers.OAuth {
    [Route("oauth/google")]
    public class OAuthController : ControllerBase {
        const string AuthUrlPattern =
            "https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={client_id}&redirect_uri={redirect_url}&prompt=consent&access_type=offline&scope={scope}";

        const string GetTokenUrlPattern =
            "https://oauth2.googleapis.com/token?grant_type=authorization_code&code={code}&client_id={client_id}&client_secret={client_secret}&redirect_uri={redirect_url}";

        const string UserInfoUrlPattern = "https://www.googleapis.com/userinfo/v2/me?access_token={access_token}";

        const string RefreshTokenUrlPattern =
            "https://oauth2.googleapis.com/token?grant_type=refresh_token&refresh_token={refresh_token}&client_id={client_id}&client_secret={client_secret}";

        static HttpClient httpClient = new();

        static PimixServiceJsonClient<GoogleAccount> serviceClient = new();

        [HttpGet("add")]
        public RedirectResult AccountAdd() {
            var targetUrl = AuthUrlPattern.Format(new Dictionary<string, string> {
                {"client_id", GoogleCloudConfigs.ClientId},
                {"redirect_url", HttpUtility.UrlEncode(this.ForAction(nameof(AccountRedirect)))},
                {"scope", HttpUtility.UrlEncode(GoogleCloudConfigs.Scope)}
            });

            return Redirect(targetUrl);
        }

        [HttpGet("redirect")]
        public ContentResult AccountRedirect([FromQuery] string code) {
            var tokenUrl = GetTokenUrlPattern.Format(new Dictionary<string, string> {
                {"code", code},
                {"client_id", GoogleCloudConfigs.ClientId},
                {"client_secret", GoogleCloudConfigs.ClientSecret},
                {"redirect_url", HttpUtility.UrlEncode(this.ForAction(nameof(AccountRedirect)))}
            });

            var response = httpClient.PostAsync(tokenUrl, null).Result.GetJToken();
            var account = new GoogleAccount {
                AccessToken = (string) response["access_token"],
                RefreshToken = (string) response["refresh_token"],
                Scope = (string) response["scope"]
            };

            var userInfoUrl =
                UserInfoUrlPattern.Format(new Dictionary<string, string> {{"access_token", account.AccessToken}});
            var info = httpClient.GetAsync(userInfoUrl).Result.GetJToken();
            account.Id = account.UserName = (string) info["email"];
            account.UserId = (string) info["id"];
            serviceClient.Set(account);
            return Content(account.ToString());
        }

        [HttpGet("refresh")]
        public ContentResult AccountRefresh([FromQuery] string id) {
            var account = serviceClient.Get(id);
            var refreshTokenUrl = RefreshTokenUrlPattern.Format(new Dictionary<string, string> {
                {"client_id", GoogleCloudConfigs.ClientId},
                {"client_secret", GoogleCloudConfigs.ClientSecret},
                {"refresh_token", account.RefreshToken}
            });

            var response = httpClient.PostAsync(refreshTokenUrl, null).Result.GetJToken();


            account.RefreshToken = (string) response["refresh_token"];
            account.AccessToken = (string) response["access_token"];

            serviceClient.Set(account);
            return Content(account.ToString());
        }
    }
}
