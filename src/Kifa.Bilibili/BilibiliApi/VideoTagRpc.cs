using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class VideoTagRpc : KifaJsonParameterizedRpc<VideoTagRpc.Response> {
    #region VideoTagRpc.Response

    public class Response {
        public long Code { get; set; }
        public string Message { get; set; }
        public long Ttl { get; set; }
        public List<Tag> Data { get; set; }
    }

    public class Tag {
        public long TagId { get; set; }
        public string TagName { get; set; }
        public string Cover { get; set; }
        public string HeadCover { get; set; }
        public string Content { get; set; }
        public string ShortContent { get; set; }
        public long Type { get; set; }
        public long State { get; set; }
        public long Ctime { get; set; }
        public Count Count { get; set; }
        public long IsAtten { get; set; }
        public long Likes { get; set; }
        public long Hates { get; set; }
        public long Attribute { get; set; }
        public long Liked { get; set; }
        public long Hated { get; set; }
        public long ExtraAttr { get; set; }
    }

    public class Count {
        public long View { get; set; }
        public long Use { get; set; }
        public long Atten { get; set; }
    }

    #endregion

    protected override string Url => "http://api.bilibili.com/x/tag/archive/tags?aid={aid}";

    protected override HttpMethod Method => HttpMethod.Get;

    public VideoTagRpc(string aid) {
        Parameters = new Dictionary<string, string> {
            { "aid", aid[2..] }
        };
    }
}
