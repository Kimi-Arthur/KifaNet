using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.Google.Rpcs;

public class ListFilesRpc : KifaJsonParameterizedRpc<ListFilesRpc.Response> {
    public class Response {
        public string? NextPageToken { get; set; }
        public required List<File> Files { get; set; }
    }

    public class File {
        public long Size { get; set; }
        public string Id { get; set; }
        public required string Name { get; set; }
    }

    protected override string Url
        => "https://www.googleapis.com/drive/v3/files?pageSize=1000&fields=nextPageToken,files/size,files/name,files/id&pageToken={page_token}&orderBy=folder,name&q='{parent_id}' in parents";

    protected override HttpMethod Method => HttpMethod.Get;

    protected override Dictionary<string, string> Headers
        => new() {
            { "Authorization", "Bearer {access_token}" }
        };

    protected override bool CamelCase => true;

    public ListFilesRpc(string parentId, string pageToken, string accessToken) {
        Parameters = new Dictionary<string, string> {
            { "parent_id", parentId },
            { "page_token", pageToken },
            { "access_token", accessToken }
        };
    }
}
