using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api;

public sealed class AddWordToLevelRpc : KifaJsonParameterizedRpc<AddWordToLevelResponse> {
    protected override string Url => "https://app.memrise.com/ajax/level/thing/add/";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override Dictionary<string, string> Headers
        => new() {
            { "referer", "{referer}" }
        };

    protected override List<KeyValuePair<string, string>> FormContent
        => new() {
            new KeyValuePair<string, string>("level_id", "{level_id}"),
            new KeyValuePair<string, string>("copy_thing_id", "{thing_id}")
        };

    public AddWordToLevelRpc(string referer, string levelId, string thingId) {
        Parameters = new Dictionary<string, string> {
            { "referer", referer },
            { "level_id", levelId },
            { "thing_id", thingId }
        };
    }
}

public class AddWordToLevelResponse {
    public bool? Success { get; set; }
}
