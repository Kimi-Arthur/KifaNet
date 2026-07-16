using System;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.BaiduCloud.Rpcs;

public sealed class DownloadFileRpc : KifaParameterizedRpc {
    protected override string Url
        => "https://pcs.baidu.com/rest/2.0/pcs/file?method=download&path={remote_path_prefix}/{remote_path}&access_token={access_token}";

    protected override HttpMethod Method => HttpMethod.Get;

    public DownloadFileRpc(string remotePathPrefix, string remotePath, string accessToken) {
        Parameters = new() {
            { "remote_path_prefix", remotePathPrefix },
            { "remote_path", Uri.EscapeDataString(remotePath.TrimStart('/')) },
            { "access_token", accessToken }
        };
    }
}
