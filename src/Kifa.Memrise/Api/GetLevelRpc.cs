using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;
using Newtonsoft.Json;

namespace Kifa.Memrise.Api;

public class GetLevelRpc : JsonRpc<GetLevelRpc.GetLevelResponse> {
    public class GetLevelResponse {
        public bool? Success { get; set; }
        public string Rendered { get; set; }
    }

    public override HttpMethod Method { get; } = HttpMethod.Get;

    public override Dictionary<string, string> Headers { get; } = new() {
        { "referer", "{referer}" }
    };

    public override string UrlPattern { get; } =
        "https://app.memrise.com/ajax/level/editing_html/?level_id={level_id}";

    public GetLevelResponse Invoke(string referer, string levelId)
        => Invoke(new Dictionary<string, string> {
            { "referer", referer },
            { "level_id", levelId }
        });
}
