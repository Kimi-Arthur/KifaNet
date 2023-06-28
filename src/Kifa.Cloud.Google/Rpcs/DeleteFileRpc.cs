using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.Google.Rpcs;

sealed class DeleteFileRpc : KifaParameterizedRpc {
    public override string UrlPattern => "https://www.googleapis.com/drive/v3/files/{file_id}";
    public override HttpMethod Method => HttpMethod.Delete;

    public override Dictionary<string, string> Headers
        => new() {
            { "Authorization", "Bearer {access_token}" }
        };

    public DeleteFileRpc(string fileId, string accessToken) {
        parameters = new Dictionary<string, string> {
            { "file_id", fileId },
            { "access_token", accessToken }
        };
    }
}
