using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

// Reference: https://github.com/SocialSisterYi/bilibili-API-collect/blob/master/docs/login/cookie_refresh.md
public sealed class ConfirmRefreshRpc : KifaJsonParameterizedRpc<ConfirmRefreshRpc.Response> {
    #region ConfirmRefreshRpc.Response

    public class Response {
        public int Code { get; set; }
        public string? Message { get; set; }
        public int Ttl { get; set; }
    }

    #endregion

    protected override string Url
        => "https://passport.bilibili.com/x/passport-login/web/confirm/refresh";

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
            new KeyValuePair<string, string>("refresh_token", "{refresh_token}")
        };

    public ConfirmRefreshRpc(string csrf, string oldRefreshToken, string? cookies = null) {
        Parameters = new() {
            { "csrf", csrf },
            { "refresh_token", oldRefreshToken }
        };

        if (cookies != null) {
            Parameters["cookies"] = cookies;
        }
    }
}
