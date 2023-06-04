using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api;

public sealed class RemoveWordRpc : KifaJsonParameterizedRpc<RemoveWordResponse> {
    public override HttpMethod Method { get; } = HttpMethod.Post;

    public override Dictionary<string, string> Headers { get; } = new() {
        { "referer", "{referer}" }
    };

    public override string UrlPattern { get; } = "https://app.memrise.com/ajax/thing/delete/";

    public override List<KeyValuePair<string, string>> FormContent { get; } = new() {
        new KeyValuePair<string, string>("thing_id", "{thingId}")
    };

    public RemoveWordRpc(string referer, string thingId) {
        parameters = new Dictionary<string, string> {
            { "thingId", thingId },
            { "referer", referer }
        };
    }
}

public class RemoveWordResponse {
    public bool? Success { get; set; }
}
