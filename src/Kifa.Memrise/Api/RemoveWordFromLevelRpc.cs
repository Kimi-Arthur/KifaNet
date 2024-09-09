using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api;

public sealed class RemoveWordFromLevelRpc : KifaJsonParameterizedRpc<RemoveWordFromLevelResponse> {
    protected override HttpMethod Method => HttpMethod.Post;

    protected override Dictionary<string, string> Headers
        => new() {
            { "referer", "{referer}" }
        };

    protected override string Url => "https://app.memrise.com/ajax/level/thing_remove/";

    protected override List<KeyValuePair<string, string>> FormContent
        => new() {
            new KeyValuePair<string, string>("level_id", "{level_id}"),
            new KeyValuePair<string, string>("thing_id", "{thing_id}")
        };

    public RemoveWordFromLevelRpc(string referer, string levelId, string thingId) {
        Parameters = new () {
            { "referer", referer },
            { "level_id", levelId },
            { "thing_id", thingId }
        };
    }
}

public class RemoveWordFromLevelResponse {
    public bool? Success { get; set; }
}
