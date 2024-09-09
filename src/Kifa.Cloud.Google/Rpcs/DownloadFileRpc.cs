using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.Google.Rpcs;

class DownloadFileRpc : KifaParameterizedRpc, KifaRpc<Stream> {
    protected override string Url
        => "https://www.googleapis.com/drive/v3/files/{file_id}?alt=media";

    protected override HttpMethod Method => HttpMethod.Get;

    protected override Dictionary<string, string> Headers
        => new() {
            { "Authorization", "Bearer {access_token}" },
            { "Range", "bytes={start_byte}-{end_byte}" }
        };

    public DownloadFileRpc(string fileId, long startByte, long endByte, string accessToken) {
        Parameters = new () {
            { "file_id", fileId },
            { "access_token", accessToken },
            { "start_byte", startByte.ToString() },
            { "end_byte", endByte.ToString() }
        };
    }

    public Stream ParseResponse(HttpResponseMessage responseMessage)
        => responseMessage.Content.ReadAsStream();
}
