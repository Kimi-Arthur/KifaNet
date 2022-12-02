using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api;

public sealed class GetLevelRpc : KifaJsonParameterizedRpc<GetLevelResponse> {
    public override HttpMethod Method { get; } = HttpMethod.Get;

    public override Dictionary<string, string> Headers { get; } = new() {
        { "referer", "{referer}" }
    };

    public override string UrlPattern { get; } =
        "https://app.memrise.com/ajax/level/editing_html/?level_id={level_id}";

    public GetLevelRpc(string referer, string levelId) {
        parameters = new Dictionary<string, string> {
            { "referer", referer },
            { "level_id", levelId }
        };
    }
}

public class GetLevelResponse {
    public bool? Success { get; set; }
    public string Rendered { get; set; }
}
