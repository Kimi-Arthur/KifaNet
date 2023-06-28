using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api;

public sealed class GetLevelRpc : KifaJsonParameterizedRpc<GetLevelResponse> {
    protected override string Url
        => "https://app.memrise.com/ajax/level/editing_html/?level_id={level_id}";

    protected override HttpMethod Method => HttpMethod.Get;

    protected override Dictionary<string, string> Headers
        => new() {
            { "referer", "{referer}" }
        };

    public GetLevelRpc(string referer, string levelId) {
        Parameters = new Dictionary<string, string> {
            { "referer", referer },
            { "level_id", levelId }
        };
    }
}

public class GetLevelResponse {
    public bool? Success { get; set; }
    public string Rendered { get; set; }
}
