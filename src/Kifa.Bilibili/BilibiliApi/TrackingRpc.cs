using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public class TrackingRpc : KifaJsonParameterizedRpc<TrackingRpc.Response> {
    #region TrackingRpc.Response

    public class Response {
        public int Code { get; set; }
        public string? Message { get; set; }
        public int Ttl { get; set; }
    }

    #endregion

    protected override string Url => "https://api.bilibili.com/x/v2/history/shadow/set";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override List<KeyValuePair<string, string>>? FormContent
        => new() {
            new KeyValuePair<string, string>("switch", "{disable_tracking}"),
            new KeyValuePair<string, string>("jsonp", "jsonp"),
            new KeyValuePair<string, string>("csrf", "{csrf_token}")
        };

    public TrackingRpc(bool disableTracking) {
        Parameters = new Dictionary<string, string> {
            { "disable_tracking", disableTracking.ToString() },
            { "csrf_token", HttpClients.BilibiliCsrfToken }
        };
    }
}
