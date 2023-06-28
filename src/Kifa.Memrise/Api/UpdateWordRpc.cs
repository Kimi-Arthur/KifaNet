using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api;

public sealed class UpdateWordRpc : KifaJsonParameterizedRpc<UpdateWordResponse> {
    protected override string Url => "https://app.memrise.com/ajax/thing/cell/update/";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override Dictionary<string, string> Headers
        => new() {
            { "referer", "{referer}" }
        };

    protected override List<KeyValuePair<string, string>> FormContent
        => new() {
            new KeyValuePair<string, string>("thing_id", "{thing_id}"),
            new KeyValuePair<string, string>("cell_id", "{cell_id}"),
            new KeyValuePair<string, string>("cell_type", "column"),
            new KeyValuePair<string, string>("new_val", "{value}")
        };

    public UpdateWordRpc(string referer, string thingId, string cellId, string value) {
        Parameters = new Dictionary<string, string> {
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
