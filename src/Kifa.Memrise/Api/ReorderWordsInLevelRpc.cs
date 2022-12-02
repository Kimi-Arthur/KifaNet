using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;
using Newtonsoft.Json;

namespace Kifa.Memrise.Api;

public sealed class ReorderWordsInLevelRpc : KifaJsonParameterizedRpc<ReorderWordsInLevelResponse> {
    public override HttpMethod Method { get; } = HttpMethod.Post;

    public override Dictionary<string, string> Headers { get; } = new() {
        { "referer", "{referer}" }
    };

    public override string UrlPattern { get; } = "https://app.memrise.com/ajax/level/reorder/";

    public override List<KeyValuePair<string, string>> FormContent { get; } = new() {
        new KeyValuePair<string, string>("level_id", "{level_id}"),
        new KeyValuePair<string, string>("thing_ids", "{thing_ids}")
    };

    public ReorderWordsInLevelRpc(string referer, string levelId, List<string> thingIds) {
        parameters = new Dictionary<string, string> {
            { "referer", referer },
            { "level_id", levelId },
            { "thing_ids", JsonConvert.SerializeObject(thingIds) }
        };
    }
}

public class ReorderWordsInLevelResponse {
    public bool? Success { get; set; }
}
