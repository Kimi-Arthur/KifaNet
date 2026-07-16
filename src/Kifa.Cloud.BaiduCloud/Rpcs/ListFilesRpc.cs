using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.BaiduCloud.Rpcs;

public sealed class ListFilesRpc : KifaJsonParameterizedRpc<ListFilesRpc.Response> {
    public class Response {
        public List<FileInformation>? List { get; set; }
    }

    public class FileInformation {
        public int Isdir { get; set; }
        public string Path { get; set; } = "";
        public long Size { get; set; }
        public string? Md5 { get; set; }
    }

    protected override string Url
        => "https://pcs.baidu.com/rest/2.0/pcs/file?method=list&path={remote_path_prefix}/{remote_path}&by=name&order=asc&access_token={access_token}";

    protected override HttpMethod Method => HttpMethod.Get;

    public ListFilesRpc(string remotePathPrefix, string remotePath, string accessToken) {
        Parameters = new() {
            { "remote_path_prefix", remotePathPrefix },
            { "remote_path", Uri.EscapeDataString(remotePath.TrimStart('/')) },
            { "access_token", accessToken }
        };
    }
}
