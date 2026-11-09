using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

// Reference: https://github.com/SocialSisterYi/bilibili-API-collect/blob/master/docs/login/cookie_refresh.md
public sealed class CookieRefreshRpc : KifaParameterizedRpc, KifaRpc<CookieRefreshRpc.Response> {
    #region CookieRefreshRpc.Response

    public class Response {
        public int Code { get; set; }
        public string? Message { get; set; }
        public int Ttl { get; set; }
        public RefreshData? Data { get; set; }
        public List<string> SetCookies { get; set; } = new();
    }

    public class RefreshData {
        public int Status { get; set; }
        public string? Message { get; set; }
        public string? RefreshToken { get; set; }
    }

    #endregion

    protected override string Url
        => "https://passport.bilibili.com/x/passport-login/web/cookie/refresh";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override Dictionary<string, string> Headers
        => Parameters.ContainsKey("cookies")
            ? new() {
                { "cookie", "{cookies}" }
            }
            : new();

    protected override List<KeyValuePair<string, string>> FormContent
        => new() {
            new KeyValuePair<string, string>("csrf", "{csrf}"),
            new KeyValuePair<string, string>("refresh_csrf", "{refresh_csrf}"),
            new KeyValuePair<string, string>("source", "main_web"),
            new KeyValuePair<string, string>("refresh_token", "{refresh_token}")
        };

    public CookieRefreshRpc(string csrf, string refreshCsrf, string refreshToken,
        string? cookies = null) {
        Parameters = new() {
            { "csrf", csrf },
            { "refresh_csrf", refreshCsrf },
            { "refresh_token", refreshToken }
        };

        if (cookies != null) {
            Parameters["cookies"] = cookies;
        }
    }

    public Response ParseResponse(HttpResponseMessage responseMessage) {
        var setCookies = responseMessage.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.ToList()
            : new List<string>();
        var result = responseMessage.GetObject<Response>().Checked();
        result.SetCookies = setCookies;
        return result;
    }
}
