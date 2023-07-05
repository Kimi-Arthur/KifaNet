using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.Google.Rpcs;

// https://developers.google.com/drive/api/reference/rest/v3/files/update
class MoveFileRpc : KifaJsonParameterizedRpc<MoveFileRpc.Response> {
    public class Response {
        public string? Kind { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? MimeType { get; set; }
    }

    protected override string Url
        => "https://www.googleapis.com/drive/v3/files/{file_id}?addParents={parent_id}";

    protected override HttpMethod Method => HttpMethod.Patch;

    protected override Dictionary<string, string> Headers
        => new() {
            { "Authorization", "Bearer {access_token}" }
        };

    protected override string? JsonContent => """{"name": "{name}"}""";

    public MoveFileRpc(string fileId, string name, string parentId, string accessToken) {
        Parameters = new Dictionary<string, string> {
            { "file_id", fileId },
            { "name", name },
            { "parent_id", parentId },
            { "access_token", accessToken }
        };
    }
}
