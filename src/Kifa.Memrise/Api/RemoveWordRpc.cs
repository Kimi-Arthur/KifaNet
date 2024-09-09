using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api;

public sealed class RemoveWordRpc : KifaJsonParameterizedRpc<RemoveWordResponse> {
    protected override string Url => "https://app.memrise.com/ajax/thing/delete/";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override Dictionary<string, string> Headers
        => new() {
            { "referer", "{referer}" }
        };

    protected override List<KeyValuePair<string, string>> FormContent
        => new() {
            new KeyValuePair<string, string>("thing_id", "{thingId}")
        };

    public RemoveWordRpc(string referer, string thingId) {
        Parameters = new () {
            { "thingId", thingId },
            { "referer", referer }
        };
    }
}

public class RemoveWordResponse {
    public bool? Success { get; set; }
}
