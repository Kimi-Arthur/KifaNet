using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.Google.Rpcs;

sealed class CreateFileRpc : KifaParameterizedRpc, KifaRpc<string> {
    public override string UrlPattern
        => "https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable";

    public override HttpMethod Method => HttpMethod.Post;

    public override Dictionary<string, string> Headers
        => new() {
            { "Authorization", "Bearer {access_token}" }
        };

    public override string JsonContent => """{"name": "{name}", "parents": ["{parent_id}"]}""";

    public CreateFileRpc(string parentId, string name, string accessToken) {
        parameters = new Dictionary<string, string> {
            { "name", name },
            { "parent_id", parentId },
            { "access_token", accessToken },
        };
    }

    public string ParseResponse(HttpResponseMessage responseMessage)
        => responseMessage.Headers.Location.Checked().ToString();
}
