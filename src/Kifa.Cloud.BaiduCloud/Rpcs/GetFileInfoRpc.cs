using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.BaiduCloud.Rpcs;

public sealed class GetFileInfoRpc : KifaJsonParameterizedRpc<GetFileInfoRpc.Response> {
    public class Response {
        public List<FileInformation>? List { get; set; }
    }

    public class FileInformation {
        public long Size { get; set; }
        public int Isdir { get; set; }
        public int Ifhassubdir { get; set; }
        public string Path { get; set; } = "";
    }

    protected override string Url
        => "https://pcs.baidu.com/rest/2.0/pcs/file?method=meta&path={remote_path_prefix}/{remote_path}&access_token={access_token}";

    protected override HttpMethod Method => HttpMethod.Get;

    public GetFileInfoRpc(string remotePathPrefix, string remotePath, string accessToken) {
        Parameters = new() {
            { "remote_path_prefix", remotePathPrefix },
            { "remote_path", Uri.EscapeDataString(remotePath.TrimStart('/')) },
            { "access_token", accessToken }
        };
    }
}
