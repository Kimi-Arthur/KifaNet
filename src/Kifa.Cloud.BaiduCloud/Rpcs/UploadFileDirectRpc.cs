using System;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.BaiduCloud.Rpcs;

public sealed class UploadFileDirectRpc : KifaJsonParameterizedRpc<UploadFileDirectRpc.Response> {
    public class Response : BaiduRpcResponse {
        public string Path { get; set; } = "";
    }

    protected override string Url
        => "https://pcs.baidu.com/rest/2.0/pcs/file?method=upload&path={remote_path_prefix}/{remote_path}&access_token={access_token}&ondup=overwrite";

    protected override HttpMethod Method => HttpMethod.Put;

    protected override byte[]? BinaryContent {
        get {
            if (offset == 0 && count == buffer.Length) {
                return buffer;
            }
            var result = new byte[count];
            Buffer.BlockCopy(buffer, offset, result, 0, count);
            return result;
        }
    }

    readonly byte[] buffer;
    readonly int offset;
    readonly int count;

    public UploadFileDirectRpc(string remotePathPrefix, string remotePath, byte[] buffer, int offset, int count, string accessToken) {
        this.buffer = buffer;
        this.offset = offset;
        this.count = count;
        Parameters = new() {
            { "remote_path_prefix", remotePathPrefix },
            { "remote_path", Uri.EscapeDataString(remotePath.TrimStart('/')) },
            { "access_token", accessToken }
        };
    }
}
