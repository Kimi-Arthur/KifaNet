using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api;

public sealed class UploadAudioRpc : KifaJsonParameterizedRpc<UpdateAudioResponse> {
    public override HttpMethod Method { get; } = HttpMethod.Post;

    public override Dictionary<string, string> Headers { get; } = new() {
        { "referer", "{referer}" }
    };

    public override Dictionary<string, Dictionary<string, string>> PartHeaders { get; } = new() {
        {
            "audio", new Dictionary<string, string> {
                { "Content-Type", "audio/mpeg" }
            }
        }
    };

    public override string UrlPattern { get; } =
        "https://app.memrise.com/ajax/thing/cell/upload_file/";

    public override List<KeyValuePair<string, string>> FormContent { get; } = new() {
        new KeyValuePair<string, string>("thing_id", "{thing_id}"),
        new KeyValuePair<string, string>("cell_id", "{cell_id}"),
        new KeyValuePair<string, string>("cell_type", "column"),
        new KeyValuePair<string, string>("csrfmiddlewaretoken", "{csrf_token}")
    };

    public override List<(string dataKey, string name, string fileName)> ExtraMultipartContent {
        get;
    } = new() {
        ("audio", "f", "f.mp3")
    };

    public UploadAudioRpc(string referer, string thingId, string cellId, string csrfToken,
        byte[] audio) {
        parameters = new Dictionary<string, string> {
            { "referer", referer },
            { "thing_id", thingId },
            { "cell_id", cellId },
            { "csrf_token", csrfToken }
        };
        byteParameters = new Dictionary<string, byte[]> {
            { "audio", audio }
        };
    }
}

public class UpdateAudioResponse {
    public bool? Success { get; set; }
    public string Rendered { get; set; }
}
