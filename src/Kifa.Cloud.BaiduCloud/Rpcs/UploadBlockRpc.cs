using System;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.BaiduCloud.Rpcs;

public sealed class UploadBlockRpc : KifaJsonParameterizedRpc<UploadBlockRpc.Response> {
    public class Response : BaiduRpcResponse {
        public string Md5 { get; set; } = "";
    }

    protected override string Url
        => "https://pcs.baidu.com/rest/2.0/pcs/file?method=upload&type=tmpfile&access_token={access_token}";

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

    public UploadBlockRpc(byte[] buffer, int offset, int count, string accessToken) {
        this.buffer = buffer;
        this.offset = offset;
        this.count = count;
        Parameters = new() {
            { "access_token", accessToken }
        };
    }
}
