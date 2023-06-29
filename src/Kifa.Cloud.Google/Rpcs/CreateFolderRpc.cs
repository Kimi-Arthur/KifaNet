using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.Google.Rpcs;

class CreateFolderRpc : KifaJsonParameterizedRpc<CreateFolderRpc.Response> {
    internal class Response {
        public required string Kind { get; set; }
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string MimeType { get; set; }
    }

    protected override string Url => "https://www.googleapis.com/drive/v3/files";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override Dictionary<string, string> Headers
        => new() {
            { "Authorization", "Bearer {access_token}" }
        };

    protected override string JsonContent
        => """{"name": "{name}", "mimeType": "application/vnd.google-apps.folder", "parents": ["{parent_id}"]}""";

    protected override bool CamelCase => true;

    public CreateFolderRpc(string parentId, string name, string accessToken) {
        Parameters = new Dictionary<string, string> {
            { "name", name },
            { "parent_id", parentId },
            { "access_token", accessToken },
        };
    }
}
