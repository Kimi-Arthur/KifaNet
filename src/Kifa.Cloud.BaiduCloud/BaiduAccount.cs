using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using Kifa.Cloud.OAuth;
using Kifa.Service;

namespace Kifa.Cloud.BaiduCloud;

public class BaiduAccount : OAuthAccount {
    public const string ModelId = "accounts/baidu";

    static KifaServiceClient<BaiduAccount>? client;

    public static KifaServiceClient<BaiduAccount> Client
        => client ??= new KifaServiceRestClient<BaiduAccount>();

    static readonly HttpClient HttpClient = new();

    public static string ClientId { get; set; }
    public static string ClientSecret { get; set; }
    public static string Scope { get; set; }
    public static RpcList Rpcs { get; set; }

    public string Userid { get; set; }
    public long TotalQuota { get; set; }
    public long UsedQuota { get; set; }

    public override string GetAuthUrl(string redirectUrl, string state)
        => Rpcs.OauthAuthorize.Url.Format(new Dictionary<string, string> {
            { "client_id", ClientId },
            { "redirect_url", HttpUtility.UrlEncode(redirectUrl) },
            { "scope", HttpUtility.UrlEncode(Scope) },
            { "state", state }
        });

    public override string GetTokenUrl(string code, string redirectUrl)
        => Rpcs.OauthToken.Url.Format(new Dictionary<string, string> {
            { "client_id", ClientId },
            { "code", code },
            { "client_secret", ClientSecret },
            { "oauth_redirect_url", redirectUrl }
        });

    public override KifaActionResult FillUserInfo() => throw new NotImplementedException();

    public override bool? Fill() {
        if (Metadata?.Freshness?.LastUpdated < DateTime.Now - TimeSpan.FromDays(30)) {
            var response = HttpClient.Send(Rpcs.OauthRefresh.GetRequest(
                new Dictionary<string, string> {
                    { "client_id", ClientId },
                    { "client_secret", ClientSecret },
                    { "refresh_token", RefreshToken },
                    { "scope", Scope }
                })).GetJToken();
            AccessToken = (string) response["access_token"];
            RefreshToken = (string) response["refresh_token"];
            return true;
        }

        return null;
    }

    public class RpcList {
        public Api OauthAuthorize { get; set; }
        public Api OauthRefresh { get; set; }
        public Api OauthToken { get; set; }
        public Api GetUserInfo { get; set; }
        public Api Quota { get; set; }
    }
}
