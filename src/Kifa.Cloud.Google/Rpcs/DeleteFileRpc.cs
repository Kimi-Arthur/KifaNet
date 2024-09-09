using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.Google.Rpcs;

class DeleteFileRpc : KifaParameterizedRpc {
    protected override string Url => "https://www.googleapis.com/drive/v3/files/{file_id}";

    protected override HttpMethod Method => HttpMethod.Delete;

    protected override Dictionary<string, string> Headers
        => new() {
            { "Authorization", "Bearer {access_token}" }
        };

    public DeleteFileRpc(string fileId, Func<string> accessTokenFunc) {
        Parameters = new () {
            { "file_id", fileId },
            { "access_token", accessTokenFunc }
        };
    }
}
