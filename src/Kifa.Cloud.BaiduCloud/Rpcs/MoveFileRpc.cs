using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.BaiduCloud.Rpcs;

public sealed class MoveFileRpc : KifaJsonParameterizedRpc<MoveFileRpc.Response> {
    public class Response : BaiduRpcResponse {
        public ExtraInfo? Extra { get; set; }
    }

    public class ExtraInfo {
        public List<Entry>? List { get; set; }
    }

    public class Entry {
        public string From { get; set; } = "";
        public string To { get; set; } = "";
    }

    protected override string Url
        => "https://pcs.baidu.com/rest/2.0/pcs/file?method=move&from={remote_path_prefix}/{from_remote_path}&to={remote_path_prefix}/{to_remote_path}&access_token={access_token}";

    protected override HttpMethod Method => HttpMethod.Post;

    public MoveFileRpc(string remotePathPrefix, string fromRemotePath, string toRemotePath, string accessToken) {
        Parameters = new() {
            { "remote_path_prefix", remotePathPrefix },
            { "from_remote_path", fromRemotePath.TrimStart('/') },
            { "to_remote_path", toRemotePath.TrimStart('/') },
            { "access_token", accessToken }
        };
    }
}
