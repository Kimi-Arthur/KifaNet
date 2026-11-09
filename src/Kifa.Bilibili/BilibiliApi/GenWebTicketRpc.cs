using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

// Reference: https://github.com/SocialSisterYi/bilibili-API-collect/blob/master/docs/misc/sign/bili_ticket.md
public sealed class GenWebTicketRpc : KifaJsonParameterizedRpc<GenWebTicketRpc.Response> {
    #region GenWebTicketRpc.Response

    public class Response {
        public int Code { get; set; }
        public string? Message { get; set; }
        public TicketData? Data { get; set; }
    }

    public class TicketData {
        public string? Ticket { get; set; }
        public long CreatedAt { get; set; }
        public long Ttl { get; set; }
    }

    #endregion

    protected override string Url
        => "https://api.bilibili.com/bapis/bilibili.api.ticket.v1.Ticket/GenWebTicket?key_id=ec02&hexsign={hexsign}&context[ts]={ts}&csrf={csrf}";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override Dictionary<string, string> Headers
        => Parameters.ContainsKey("cookies")
            ? new() {
                { "cookie", "{cookies}" }
            }
            : new();

    const string HmacKey = "XgwSnGZ1p";

    public GenWebTicketRpc(string? csrf = null, string? cookies = null, long? timestamp = null) {
        var ts = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(HmacKey),
            Encoding.UTF8.GetBytes($"ts{ts}"));
        var hexsign = Convert.ToHexString(hash).ToLowerInvariant();

        Parameters = new() {
            { "hexsign", hexsign },
            { "ts", ts.ToString() },
            { "csrf", csrf ?? "" }
        };

        if (cookies != null) {
            Parameters["cookies"] = cookies;
        }
    }
}
