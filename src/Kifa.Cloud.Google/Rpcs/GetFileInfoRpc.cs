using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.Google.Rpcs;

class GetFileInfoRpc : KifaJsonParameterizedRpc<GetFileInfoRpc.Response> {
    public class Response {
        public required List<string> Parents { get; set; }
        public long Size { get; set; }
    }

    protected override string Url
        => "https://www.googleapis.com/drive/v3/files/{file_id}?fields=size,parents";

    protected override HttpMethod Method => HttpMethod.Get;

    protected override Dictionary<string, string> Headers
        => new() {
            { "Authorization", "Bearer {access_token}" }
        };

    protected override bool CamelCase => true;

    public GetFileInfoRpc(string fileId, Func<string> accessTokenFunc) {
        Parameters = new () {
            { "file_id", fileId },
            { "access_token", accessTokenFunc },
        };
    }
}
