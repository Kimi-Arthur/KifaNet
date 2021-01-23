using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using Kifa.Cloud.GooglePhotos.PhotosApi;
using Kifa.Cloud.OAuth;
using Pimix;
using Pimix.Service;

namespace Kifa.Cloud.GooglePhotos {
    public class GoogleAccount : OAuthAccount {
        public const string ModelId = "accounts/google";

        static PimixServiceClient<GoogleAccount> client;

        public static PimixServiceClient<GoogleAccount> Client =>
            client ??= new PimixServiceRestClient<GoogleAccount>();

        static readonly HttpClient HttpClient = new HttpClient();

        const string DefaultScope =
            "openid https://www.googleapis.com/auth/drive https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/photoslibrary";

        const string AuthUrlPattern =
            "https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={client_id}&redirect_uri={redirect_url}&prompt=consent&access_type=offline&scope={scope}&state={state}";

        const string GetTokenUrlPattern =
            "https://oauth2.googleapis.com/token?grant_type=authorization_code&code={code}&client_id={client_id}&client_secret={client_secret}&redirect_uri={redirect_url}";

        const string UserInfoUrlPattern = "https://www.googleapis.com/userinfo/v2/me?access_token={access_token}";

        const string RefreshTokenUrlPattern =
            "https://oauth2.googleapis.com/token?grant_type=refresh_token&refresh_token={refresh_token}&client_id={client_id}&client_secret={client_secret}";

        public override string GetAuthUrl(string redirectUrl, string state) =>
            AuthUrlPattern.Format(new Dictionary<string, string> {
                {"client_id", GoogleCloudConfigs.ClientId},
                {"redirect_url", HttpUtility.UrlEncode(redirectUrl)},
                {"scope", HttpUtility.UrlEncode(DefaultScope)},
                {"state", state}
            });

        public override string GetTokenUrl(string code, string redirectUrl) {
            return GetTokenUrlPattern.Format(new Dictionary<string, string> {
                {"code", code},
                {"client_id", GoogleCloudConfigs.ClientId},
                {"client_secret", GoogleCloudConfigs.ClientSecret},
                {"redirect_url", HttpUtility.UrlEncode(redirectUrl)}
            });
        }

        public override RestActionResult FillUserInfo() =>
            RestActionResult.FromAction(() => {
                var userInfoUrl =
                    UserInfoUrlPattern.Format(new Dictionary<string, string> {{"access_token", AccessToken}});
                var info = HttpClient.GetAsync(userInfoUrl).Result.GetJToken();
                UserName = (string) info["email"];
                UserId = (string) info["id"];
            });

        public override RestActionResult RefreshAccount() {
            return RestActionResult.FromAction(() => {
                var refreshTokenUrl = RefreshTokenUrlPattern.Format(new Dictionary<string, string> {
                    {"client_id", GoogleCloudConfigs.ClientId},
                    {"client_secret", GoogleCloudConfigs.ClientSecret},
                    {"refresh_token", RefreshToken}
                });

                var response = HttpClient.PostAsync(refreshTokenUrl, null).Result.GetJToken();
                AccessToken = (string) response["access_token"];
            });
        }
    }
}
