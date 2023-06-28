using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api;

public sealed class RemoveAudioRpc : KifaJsonParameterizedRpc<RemoveAudioResponse> {
    protected override string Url => "https://app.memrise.com/ajax/thing/column/delete_from/";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override Dictionary<string, string> Headers
        => new() {
            { "referer", "{referer}" }
        };

    protected override List<KeyValuePair<string, string>> FormContent
        => new() {
            new KeyValuePair<string, string>("thing_id", "{thing_id}"),
            new KeyValuePair<string, string>("column_key", "{audio_column}"),
            new KeyValuePair<string, string>("file_id", "{file_id}"),
            new KeyValuePair<string, string>("cell_type", "column")
        };

    public RemoveAudioRpc(string referer, string thingId, string audioColumn, string fileId) {
        Parameters = new Dictionary<string, string> {
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
