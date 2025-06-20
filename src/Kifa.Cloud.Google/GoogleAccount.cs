using System;
using System.Net.Http;
using System.Web;
using Kifa.Cloud.Google.Rpcs;
using Kifa.Cloud.OAuth;
using Kifa.Service;

namespace Kifa.Cloud.Google;

public class GoogleAccount : OAuthAccount, WithModelId<GoogleAccount> {
    public static string ModelId => "accounts/google";

    static readonly TimeSpan TokenValidDuration = TimeSpan.FromHours(1) - TimeSpan.FromMinutes(5);

    public static KifaServiceClient<GoogleAccount> Client { get; set; } =
        new KifaServiceRestClient<GoogleAccount>();

    static readonly HttpClient HttpClient = new();

    const string DefaultScope =
        "openid https://www.googleapis.com/auth/drive https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/photoslibrary";

    const string AuthUrlPattern =
        "https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={client_id}&redirect_uri={redirect_url}&prompt=consent&access_type=offline&scope={scope}&state={state}";

    const string UserInfoUrlPattern =
        "https://www.googleapis.com/userinfo/v2/me?access_token={access_token}";

    const string RefreshTokenUrlPattern =
        "https://oauth2.googleapis.com/token?grant_type=refresh_token&refresh_token={refresh_token}&client_id={client_id}&client_secret={client_secret}";

    public override string GetAuthUrl(string redirectUrl, string state)
        => AuthUrlPattern.Format(("client_id", GoogleCloudConfig.ClientId),
            ("redirect_url", HttpUtility.UrlEncode(redirectUrl)),
            ("scope", HttpUtility.UrlEncode(DefaultScope)), ("state", state));

    public override OAuthTokenRequest GetTokenRequest(string code, string redirectUrl)
        => new GoogleOAuthTokenRequest(code: code, clientId: GoogleCloudConfig.ClientId,
            clientSecret: GoogleCloudConfig.ClientSecret,
            redirectUrl: HttpUtility.UrlEncode(redirectUrl));

    public override KifaActionResult FillUserInfo()
        => KifaActionResult.FromAction(() => {
            var userInfoUrl = UserInfoUrlPattern.Format(("access_token", AccessToken));
            var info =
                HttpClient.FetchJToken(() => new HttpRequestMessage(HttpMethod.Get, userInfoUrl));
            UserName = (string) info["email"];
            UserId = (string) info["id"];
        });

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        if (string.IsNullOrEmpty(RefreshToken)) {
            throw new DataNotFoundException("No refresh token found.");
        }

        var refreshTokenUrl = RefreshTokenUrlPattern.Format(
            ("client_id", GoogleCloudConfig.ClientId),
            ("client_secret", GoogleCloudConfig.ClientSecret), ("refresh_token", RefreshToken));

        var response =
            HttpClient.FetchJToken(() => new HttpRequestMessage(HttpMethod.Post, refreshTokenUrl));
        var token = (string) response["access_token"];

        AccessToken = token ?? throw new InvalidOperationException("Refresh is not successful.");

        return DateTimeOffset.UtcNow + TokenValidDuration;
    }
}
