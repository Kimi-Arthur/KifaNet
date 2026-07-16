using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.BaiduCloud.Rpcs;

public sealed class DiffFileListRpc : KifaJsonParameterizedRpc<DiffFileListRpc.Response> {
    public class Response {
        public string Cursor { get; set; } = "";
        public bool HasMore { get; set; }
        public bool Reset { get; set; }
        public bool ClientTypeReset { get; set; }
        public string RequestCursor { get; set; } = "";
        public long RequestId { get; set; }
        public Dictionary<string, FileInformation> Entries { get; set; } = new();
    }

    public class FileInformation {
        public int Isdir { get; set; }
        public string Path { get; set; } = "";
        public long Size { get; set; }
        public string? Md5 { get; set; }
        public int Isdelete { get; set; }
        public long Ctime { get; set; }
        public long Mtime { get; set; }
        public long FsId { get; set; }
        public long OwnerId { get; set; }
        public int OwnerType { get; set; }
        public long Revision { get; set; }
    }

    protected override string Url
        => "https://pcs.baidu.com/rest/2.0/pcs/file?method=diff&cursor={cursor}&access_token={access_token}";

    protected override HttpMethod Method => HttpMethod.Get;

    public DiffFileListRpc(string cursor, string accessToken) {
        Parameters = new() {
            { "cursor", cursor },
            { "access_token", accessToken }
        };
    }
}
