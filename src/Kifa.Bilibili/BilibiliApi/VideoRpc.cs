using System;
using System.Collections.Generic;
using System.Net.Http;
using Pimix;

namespace Kifa.Bilibili.BilibiliApi {
    public class VideoRpc : JsonRpc<string, VideoRpc.VideoResponse> {
        public class VideoResponse {
            public long Code { get; set; }
            public long Message { get; set; }
            public long Ttl { get; set; }
            public Data Data { get; set; }
        }

        public class Data {
            public string Bvid { get; set; }
            public long Aid { get; set; }
            public long Videos { get; set; }
            public long Tid { get; set; }
            public string Tname { get; set; }
            public long Copyright { get; set; }
            public Uri Pic { get; set; }
            public string Title { get; set; }
            public long Pubdate { get; set; }
            public long Ctime { get; set; }
            public string Desc { get; set; }
            public long State { get; set; }
            public long Duration { get; set; }
            public Dictionary<string, long> Rights { get; set; }
            public Owner Owner { get; set; }
            public Stat Stat { get; set; }
            public string Dynamic { get; set; }
            public long Cid { get; set; }
            public Dimension Dimension { get; set; }
            public bool NoCache { get; set; }
            public List<PageType> Pages { get; set; }
            public Subtitle Subtitle { get; set; }
            public UserGarb UserGarb { get; set; }
        }

        public class Dimension {
            public int Width { get; set; }
            public int Height { get; set; }
            public int Rotate { get; set; }
        }

        public class Owner {
            public long Mid { get; set; }
            public string Name { get; set; }
            public Uri Face { get; set; }
        }

        public class PageType {
            public long Cid { get; set; }
            public int Page { get; set; }
            public string From { get; set; }
            public string Part { get; set; }
            public long Duration { get; set; }
            public string Vid { get; set; }
            public string Weblink { get; set; }
            public Dimension Dimension { get; set; }
        }

        public class Stat {
            public long Aid { get; set; }
            public long View { get; set; }
            public long Danmaku { get; set; }
            public long Reply { get; set; }
            public long Favorite { get; set; }
            public long Coin { get; set; }
            public long Share { get; set; }
            public long NowRank { get; set; }
            public long HisRank { get; set; }
            public long Like { get; set; }
            public long Dislike { get; set; }
            public string Evaluation { get; set; }
            public string ArgueMsg { get; set; }
        }

        public class Subtitle {
            public bool AllowSubmit { get; set; }
            public List<object> List { get; set; }
        }

        public class UserGarb {
            public string UrlImageAniCut { get; set; }
        }

        const string VideoInfoPattern = "https://api.bilibili.com/x/web-interface/view?aid={aid}";

        static HttpClient client = BilibiliVideo.GetBilibiliClient();

        public override VideoResponse Call(string aid) {
            var url = VideoInfoPattern.Format(new Dictionary<string, string> {{"aid", aid.Substring(2)}});
            return client.GetAsync(url).Result.GetObject<VideoResponse>();
        }
    }
}