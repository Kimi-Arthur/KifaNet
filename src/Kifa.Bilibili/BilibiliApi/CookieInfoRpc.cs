using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

// Reference: https://github.com/SocialSisterYi/bilibili-API-collect/blob/master/docs/login/cookie_refresh.md
public sealed class CookieInfoRpc : KifaJsonParameterizedRpc<CookieInfoRpc.Response> {
    #region CookieInfoRpc.Response

    public class Response {
        public int Code { get; set; }
        public string? Message { get; set; }
        public int Ttl { get; set; }
        public CookieInfoData? Data { get; set; }
    }

    public class CookieInfoData {
        public bool Refresh { get; set; }
        public long Timestamp { get; set; }
    }

    #endregion

    protected override string Url
        => "https://passport.bilibili.com/x/passport-login/web/cookie/info?csrf={csrf}";

    protected override HttpMethod Method => HttpMethod.Get;

    protected override Dictionary<string, string> Headers
        => Parameters.ContainsKey("cookies")
            ? new() {
                { "cookie", "{cookies}" }
            }
            : new();

    public CookieInfoRpc(string? csrf = null, string? cookies = null) {
        Parameters = new() {
            { "csrf", csrf ?? "" }
        };

        if (cookies != null) {
            Parameters["cookies"] = cookies;
        }
    }
}
