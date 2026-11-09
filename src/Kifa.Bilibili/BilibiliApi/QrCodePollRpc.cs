using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

// Reference: https://github.com/SocialSisterYi/bilibili-API-collect/blob/master/docs/login/login_action/QR.md
public sealed class QrCodePollRpc : KifaParameterizedRpc, KifaRpc<QrCodePollRpc.Response> {
    #region QrCodePollRpc.Response

    public class Response {
        public int Code { get; set; }
        public string? Message { get; set; }
        public int Ttl { get; set; }
        public PollData? Data { get; set; }
        public List<string> SetCookies { get; set; } = new();
    }

    public class PollData {
        // 86038: 二维码已失效 (QR code expired).
        // Reference: https://github.com/SocialSisterYi/bilibili-API-collect/blob/master/docs/login/login_action/QR.md
        public const int ExpiredCode = 86038;

        public string? Url { get; set; }
        public string? RefreshToken { get; set; }
        public long Timestamp { get; set; }
        public int Code { get; set; }
        public string? Message { get; set; }
    }

    #endregion

    protected override string Url
        => "https://passport.bilibili.com/x/passport-login/web/qrcode/poll?qrcode_key={qrcode_key}";

    protected override HttpMethod Method => HttpMethod.Get;

    public QrCodePollRpc(string qrcodeKey) {
        Parameters = new() {
            { "qrcode_key", qrcodeKey }
        };
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
