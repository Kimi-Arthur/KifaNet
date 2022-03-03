using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using Kifa.Cloud.OAuth;
using Kifa.Service;

namespace Kifa.Cloud.Google;

public class GoogleAccount : OAuthAccount {
    public const string ModelId = "accounts/google";

    static KifaServiceClient<GoogleAccount>? client;

    public static KifaServiceClient<GoogleAccount> Client
        => client ??= new KifaServiceRestClient<GoogleAccount>();

    static readonly HttpClient HttpClient = new();

    const string DefaultScope =
        "openid https://www.googleapis.com/auth/drive https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/photoslibrary";

    const string AuthUrlPattern =
        "https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={client_id}&redirect_uri={redirect_url}&prompt=consent&access_type=offline&scope={scope}&state={state}";

    const string GetTokenUrlPattern =
        "https://oauth2.googleapis.com/token?grant_type=authorization_code&code={code}&client_id={client_id}&client_secret={client_secret}&redirect_uri={redirect_url}";

    const string UserInfoUrlPattern =
        "https://www.googleapis.com/userinfo/v2/me?access_token={access_token}";

    const string RefreshTokenUrlPattern =
        "https://oauth2.googleapis.com/token?grant_type=refresh_token&refresh_token={refresh_token}&client_id={client_id}&client_secret={client_secret}";

    public override string GetAuthUrl(string redirectUrl, string state)
        => AuthUrlPattern.Format(new Dictionary<string, string> {
            { "client_id", GoogleCloudConfig.ClientId },
            { "redirect_url", HttpUtility.UrlEncode(redirectUrl) },
            { "scope", HttpUtility.UrlEncode(DefaultScope) },
            { "state", state }
        });

    public override string GetTokenUrl(string code, string redirectUrl)
        => GetTokenUrlPattern.Format(new Dictionary<string, string> {
            { "code", code },
            { "client_id", GoogleCloudConfig.ClientId },
            { "client_secret", GoogleCloudConfig.ClientSecret },
            { "redirect_url", HttpUtility.UrlEncode(redirectUrl) }
        });

    public override KifaActionResult FillUserInfo()
        => KifaActionResult.FromAction(() => {
            var userInfoUrl = UserInfoUrlPattern.Format(new Dictionary<string, string> {
                { "access_token", AccessToken }
            });
            var info = HttpClient.GetAsync(userInfoUrl).Result.GetJToken();
            UserName = (string) info["email"];
            UserId = (string) info["id"];
        });

    public override KifaActionResult RefreshAccount()
        => KifaActionResult.FromAction(() => {
            var refreshTokenUrl = RefreshTokenUrlPattern.Format(new Dictionary<string, string> {
                { "client_id", GoogleCloudConfig.ClientId },
                { "client_secret", GoogleCloudConfig.ClientSecret },
                { "refresh_token", RefreshToken }
            });

            var response = HttpClient.PostAsync(refreshTokenUrl, null).Result.GetJToken();
            AccessToken = (string) response["access_token"];
        });
}
