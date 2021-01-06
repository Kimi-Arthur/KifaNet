using System.Collections.Generic;
using System.Net.Http;

namespace Pimix.Bilibili.BilibiliApi {
    public class VideoTagRpc : JsonRpc<string, VideoTagRpc.VideoTagResponse> {
        public class VideoTagResponse {
            public long Code { get; set; }
            public long Message { get; set; }
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

        const string VideoTagPattern = "http://api.bilibili.com/x/tag/archive/tags?aid={aid}";

        static HttpClient client = BilibiliVideo.GetBilibiliClient();

        public override VideoTagResponse Call(string aid) {
            var url = VideoTagPattern.Format(new Dictionary<string, string> {{"aid", aid.Substring(2)}});
            return client.GetAsync(url).Result.GetObject<VideoTagResponse>();
        }
    }
}
