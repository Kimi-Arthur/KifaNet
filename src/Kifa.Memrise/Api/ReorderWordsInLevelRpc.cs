using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;
using Newtonsoft.Json;

namespace Kifa.Memrise.Api;

public sealed class ReorderWordsInLevelRpc : KifaJsonParameterizedRpc<ReorderWordsInLevelResponse> {
    protected override string Url => "https://app.memrise.com/ajax/level/reorder/";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override Dictionary<string, string> Headers
        => new() {
            { "referer", "{referer}" }
        };

    protected override List<KeyValuePair<string, string>> FormContent
        => new() {
            new KeyValuePair<string, string>("level_id", "{level_id}"),
            new KeyValuePair<string, string>("thing_ids", "{thing_ids}")
        };

    public ReorderWordsInLevelRpc(string referer, string levelId, List<string> thingIds) {
        Parameters = new Dictionary<string, string> {
            { "referer", referer },
            { "level_id", levelId },
            { "thing_ids", JsonConvert.SerializeObject(thingIds) }
        };
    }
}

public class ReorderWordsInLevelResponse {
    public bool? Success { get; set; }
}
