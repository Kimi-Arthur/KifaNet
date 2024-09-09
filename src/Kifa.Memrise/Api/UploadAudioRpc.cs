using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api;

public sealed class UploadAudioRpc : KifaJsonParameterizedRpc<UpdateAudioResponse> {
    protected override string Url => "https://app.memrise.com/ajax/thing/cell/upload_file/";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override Dictionary<string, string> Headers
        => new() {
            { "referer", "{referer}" }
        };

    protected override Dictionary<string, Dictionary<string, string>> PartHeaders
        => new() {
            {
                "audio", new Dictionary<string, string> {
                    { "Content-Type", "audio/mpeg" }
                }
            }
        };

    protected override List<KeyValuePair<string, string>> FormContent
        => new() {
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
        Parameters = new() {
            { "referer", referer },
            { "thing_id", thingId },
            { "cell_id", cellId },
            { "csrf_token", csrfToken }
        };
        ByteParameters = new() {
            { "audio", audio }
        };
    }
}

public class UpdateAudioResponse {
    public bool? Success { get; set; }
    public string Rendered { get; set; }
}
