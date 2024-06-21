using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using Kifa.Rpc;

namespace Kifa.Cloud.Google.Rpcs;

class FindFileRpc : KifaJsonParameterizedRpc<FindFileRpc.Response> {
    public class Response {
        public string? Kind { get; set; }
        public bool IncompleteSearch { get; set; }
        public List<File> Files { get; set; } = new();
    }

    public class File {
        public string? Kind { get; set; }
        public string? MimeType { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    protected override string Url
        => "https://www.googleapis.com/drive/v3/files?q=name = '{name}' and '{parent_id}' in parents";

    protected override HttpMethod Method => HttpMethod.Get;

    protected override Dictionary<string, string> Headers
        => new() {
            { "Authorization", "Bearer {access_token}" }
        };

    protected override bool CamelCase => true;

    public FindFileRpc(string parentId, string name, string accessToken) {
        Parameters = new Dictionary<string, string> {
            { "parent_id", parentId },
            { "name", HttpUtility.UrlEncode(name) },
            { "access_token", accessToken }
        };
    }
}
