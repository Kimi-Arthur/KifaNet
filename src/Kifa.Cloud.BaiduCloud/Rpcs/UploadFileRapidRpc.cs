using System;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.BaiduCloud.Rpcs;

public sealed class UploadFileRapidRpc : KifaJsonParameterizedRpc<UploadFileRapidRpc.Response> {
    public class Response : BaiduRpcResponse {
        public string Md5 { get; set; } = "";
    }

    protected override string Url
        => "https://pcs.baidu.com/rest/2.0/pcs/file?method=rapidupload&path={remote_path_prefix}/{remote_path}&content-length={content_length}&content-md5={content_md5}&slice-md5={slice_md5}&content-crc32={content_crc32}&access_token={access_token}&ondup=overwrite";

    protected override HttpMethod Method => HttpMethod.Post;

    public UploadFileRapidRpc(string remotePathPrefix, string remotePath, long contentLength, string contentMd5, string sliceMd5, string contentCrc32, string accessToken) {
        Parameters = new() {
            { "remote_path_prefix", remotePathPrefix },
            { "remote_path", Uri.EscapeDataString(remotePath.TrimStart('/')) },
            { "content_length", contentLength.ToString() },
            { "content_md5", contentMd5 },
            { "slice_md5", sliceMd5 },
            { "content_crc32", contentCrc32 },
            { "access_token", accessToken }
        };
    }
}
