using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;
using Newtonsoft.Json;

namespace Kifa.Cloud.BaiduCloud.Rpcs;

public sealed class MergeBlocksRpc : KifaJsonParameterizedRpc<MergeBlocksRpc.Response> {
    public class Response {
        public string Path { get; set; } = "";
    }

    protected override string Url
        => "https://pcs.baidu.com/rest/2.0/pcs/file?method=createsuperfile&path={remote_path_prefix}/{remote_path}&access_token={access_token}&ondup=overwrite";

    protected override HttpMethod Method => HttpMethod.Post;

    readonly List<string> blockList;

    public MergeBlocksRpc(string remotePathPrefix, string remotePath, List<string> blockList, string accessToken) {
        this.blockList = blockList;
        Parameters = new() {
            { "remote_path_prefix", remotePathPrefix },
            { "remote_path", Uri.EscapeDataString(remotePath.TrimStart('/')) },
            { "access_token", accessToken }
        };
    }

    protected override List<KeyValuePair<string, string>> FormContent => new() {
        new("param", JsonConvert.SerializeObject(new Dictionary<string, List<string>> {
            ["block_list"] = blockList
        }))
    };
}
