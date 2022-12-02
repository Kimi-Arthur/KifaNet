using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api;

public sealed class UpdateWordRpc : KifaJsonParameterizedRpc<UpdateWordResponse> {
    public override HttpMethod Method { get; } = HttpMethod.Post;

    public override Dictionary<string, string> Headers { get; } = new() {
        { "referer", "{referer}" }
    };

    public override string UrlPattern { get; } = "https://app.memrise.com/ajax/thing/cell/update/";

    public override List<KeyValuePair<string, string>> FormContent { get; } = new() {
        new KeyValuePair<string, string>("thing_id", "{thing_id}"),
        new KeyValuePair<string, string>("cell_id", "{cell_id}"),
        new KeyValuePair<string, string>("cell_type", "column"),
        new KeyValuePair<string, string>("new_val", "{value}")
    };

    public UpdateWordRpc(string referer, string thingId, string cellId, string value) {
        parameters = new Dictionary<string, string> {
            { "referer", referer },
            { "thing_id", thingId },
            { "cell_id", cellId },
            { "value", value }
        };
    }
}

public class UpdateWordResponse {
    public bool? Success { get; set; }
}
