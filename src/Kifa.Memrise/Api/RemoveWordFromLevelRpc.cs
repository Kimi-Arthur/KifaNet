using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api;

public sealed class RemoveWordFromLevelRpc : KifaJsonParameterizedRpc<RemoveWordFromLevelResponse> {
    public override HttpMethod Method { get; } = HttpMethod.Post;

    public override Dictionary<string, string> Headers { get; } = new() {
        { "referer", "{referer}" }
    };

    public override string UrlPattern { get; } = "https://app.memrise.com/ajax/level/thing_remove/";

    public override List<KeyValuePair<string, string>> FormContent { get; } = new() {
        new KeyValuePair<string, string>("level_id", "{level_id}"),
        new KeyValuePair<string, string>("thing_id", "{thing_id}")
    };

    public RemoveWordFromLevelRpc(string referer, string levelId, string thingId) {
        parameters = new Dictionary<string, string> {
            { "referer", referer },
            { "level_id", levelId },
            { "thing_id", thingId }
        };
    }
}

public class RemoveWordFromLevelResponse {
    public bool? Success { get; set; }
}
