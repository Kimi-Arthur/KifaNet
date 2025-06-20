using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using Kifa.Cloud.BaiduCloud.Rpcs;
using Kifa.Cloud.OAuth;
using Kifa.Service;

namespace Kifa.Cloud.BaiduCloud;

public class BaiduAccount : OAuthAccount, WithModelId<BaiduAccount> {
    public static string ModelId => "accounts/baidu";

    static readonly TimeSpan TokenValidDuration = TimeSpan.FromDays(30) - TimeSpan.FromHours(1);

    public static KifaServiceClient<BaiduAccount> Client { get; set; } =
        new KifaServiceRestClient<BaiduAccount>();

    static readonly HttpClient HttpClient = new();

    public static string ClientId { get; set; }
    public static string ClientSecret { get; set; }
    public static string Scope { get; set; }
    public static RpcList Rpcs { get; set; }

    public string Userid { get; set; }
    public long TotalQuota { get; set; }
    public long UsedQuota { get; set; }

    public override string GetAuthUrl(string redirectUrl, string state)
        => Rpcs.OauthAuthorize.Url.Format(("client_id", ClientId),
            ("redirect_url", HttpUtility.UrlEncode(redirectUrl)),
            ("scope", HttpUtility.UrlEncode(Scope)), ("state", state));

    public override OAuthTokenRequest GetTokenRequest(string code, string redirectUrl)
        => new BaiduOAuthTokenRequest(code: code, clientId: ClientId, clientSecret: ClientSecret,
            redirectUrl: redirectUrl);

    public override KifaActionResult FillUserInfo() => throw new NotImplementedException();

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        var response = HttpClient.FetchJToken(() => Rpcs.OauthRefresh.GetRequest(
            ("client_id", ClientId), ("client_secret", ClientSecret),
            ("refresh_token", RefreshToken), ("scope", Scope)));
        AccessToken = (string) response["access_token"];
        RefreshToken = (string) response["refresh_token"];

        return DateTimeOffset.UtcNow + TokenValidDuration;
    }

    public class RpcList {
        public Api OauthAuthorize { get; set; }
        public Api OauthRefresh { get; set; }
        public Api OauthToken { get; set; }
        public Api GetUserInfo { get; set; }
        public Api Quota { get; set; }
    }
}
