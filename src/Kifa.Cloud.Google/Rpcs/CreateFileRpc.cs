using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.Google.Rpcs;

class CreateFileRpc : KifaParameterizedRpc, KifaRpc<string> {
    protected override string Url
        => "https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override Dictionary<string, string> Headers
        => new() {
            { "Authorization", "Bearer {access_token}" }
        };

    protected override string JsonContent => """{"name": "{name}", "parents": ["{parent_id}"]}""";

    public CreateFileRpc(string parentId, string name, string accessToken) {
        Parameters = new () {
            { "parent_id", parentId },
            { "name", name },
            { "access_token", accessToken },
        };
    }

    public string ParseResponse(HttpResponseMessage responseMessage)
        => responseMessage.Headers.Location.Checked().ToString();
}
