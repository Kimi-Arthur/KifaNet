using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.Google.Rpcs;

public class GetFileInfoRpc : KifaJsonParameterizedRpc<GetFileInfoRpc.Response> {
    public class Response {
        public long Size { get; set; }
    }

    protected override string Url
        => "https://www.googleapis.com/drive/v3/files/{file_id}?fields=size";

    protected override HttpMethod Method => HttpMethod.Get;

    protected override Dictionary<string, string> Headers
        => new() {
            { "Authorization", "Bearer {access_token}" }
        };

    public GetFileInfoRpc(string fileId, string accessToken) {
        Parameters = new Dictionary<string, string> {
            { "file_id", fileId },
            { "access_token", accessToken },
        };
    }
}
