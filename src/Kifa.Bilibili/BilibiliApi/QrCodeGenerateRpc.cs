using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

// Reference: https://github.com/SocialSisterYi/bilibili-API-collect/blob/master/docs/login/login_action/QR.md
public sealed class QrCodeGenerateRpc : KifaJsonParameterizedRpc<QrCodeGenerateRpc.Response> {
    #region QrCodeGenerateRpc.Response

    public class Response {
        public int Code { get; set; }
        public string? Message { get; set; }
        public int Ttl { get; set; }
        public QrCodeData? Data { get; set; }
    }

    public class QrCodeData {
        public string? Url { get; set; }
        public string? QrcodeKey { get; set; }
    }

    #endregion

    protected override string Url
        => "https://passport.bilibili.com/x/passport-login/web/qrcode/generate";

    protected override HttpMethod Method => HttpMethod.Get;
}
