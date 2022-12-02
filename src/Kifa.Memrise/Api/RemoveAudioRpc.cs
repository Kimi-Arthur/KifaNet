using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api;

public sealed class RemoveAudioRpc : KifaJsonParameterizedRpc<RemoveAudioResponse> {
    public override string UrlPattern { get; } =
        "https://app.memrise.com/ajax/thing/column/delete_from/";

    public override HttpMethod Method { get; } = HttpMethod.Post;

    public override Dictionary<string, string> Headers { get; } = new() {
        { "referer", "{referer}" }
    };

    public override List<KeyValuePair<string, string>> FormContent { get; } = new() {
        new KeyValuePair<string, string>("thing_id", "{thing_id}"),
        new KeyValuePair<string, string>("column_key", "{audio_column}"),
        new KeyValuePair<string, string>("file_id", "{file_id}"),
        new KeyValuePair<string, string>("cell_type", "column")
    };

    public RemoveAudioRpc(string referer, string thingId, string audioColumn, string fileId) {
        parameters = new Dictionary<string, string> {
            { "referer", referer },
            { "thing_id", thingId },
            { "audio_column", audioColumn },
            { "file_id", fileId }
        };
    }
}

public class RemoveAudioResponse {
    public bool Success { get; set; }
    public string Rendered { get; set; }
}
