using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;
using Newtonsoft.Json;

namespace Kifa.Memrise.Api;

public class ReorderWordsInLevelRpc : JsonRpc<ReorderWordsInLevelRpc.ReorderWordsInLevelResponse> {
    public class ReorderWordsInLevelResponse {
        public bool? Success { get; set; }
    }

    public override HttpMethod Method { get; } = HttpMethod.Post;

    public override Dictionary<string, string> Headers { get; } = new() {
        { "referer", "{referer}" }
    };

    public override string UrlPattern { get; } = "https://app.memrise.com/ajax/level/reorder/";

    public override List<KeyValuePair<string, string>> FormContent { get; set; } = new() {
        new KeyValuePair<string, string>("level_id", "{level_id}"),
        new KeyValuePair<string, string>("thing_ids", "{thing_ids}")
    };

    public ReorderWordsInLevelResponse Invoke(string referer, string levelId, List<string> thingIds)
        => Invoke(new Dictionary<string, string> {
            { "referer", referer },
            { "level_id", levelId },
            { "thing_ids", JsonConvert.SerializeObject(thingIds) }
        });
}
