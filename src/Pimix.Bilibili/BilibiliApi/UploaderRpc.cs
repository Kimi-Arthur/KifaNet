using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Pimix.Service;

namespace Pimix.Bilibili.BilibiliApi {
    public class UploaderRpc : JsonRpc<string, UploaderRpc.UploaderResponse> {
        public class UploaderResponse {
            public long Code { get; set; }
            public string Message { get; set; }
            public long Ttl { get; set; }
            public Data Data { get; set; }
        }

        public class Data {
            public List List { get; set; }
            public Page Page { get; set; }
            public EpisodicButton EpisodicButton { get; set; }
        }

        public class EpisodicButton {
            public string Text { get; set; }
            public string Uri { get; set; }
        }

        public class List {
            public Dictionary<string, VideoType> Tlist { get; set; }
            public List<Video> Vlist { get; set; }
        }

        public class VideoType {
            public long Tid { get; set; }
            public long Count { get; set; }
            public string Name { get; set; }
        }

        public class Video {
            public long Comment { get; set; }
            public long Typeid { get; set; }
            // This field can be string...
            // public long Play { get; set; }
            public string Pic { get; set; }
            public string Subtitle { get; set; }
            public string Description { get; set; }
            public string Copyright { get; set; }
            public string Title { get; set; }
            public long Review { get; set; }
            public string Author { get; set; }
            public long Mid { get; set; }
            public long Created { get; set; }
            public string Length { get; set; }
            public long VideoReview { get; set; }
            public long Aid { get; set; }
            public string Bvid { get; set; }
            public bool HideClick { get; set; }
            public long IsPay { get; set; }
            public long IsUnionVideo { get; set; }
            public long IsSteinsGate { get; set; }
        }

        public class Page {
            public long Pn { get; set; }
            public long Ps { get; set; }
            public long Count { get; set; }
        }

        const string UploaderUrlPattern = "https://api.bilibili.com/x/space/arc/search?mid={id}&ps=100&pn={page}";

        static HttpClient client = BilibiliVideo.GetBilibiliClient();

        public override UploaderResponse Call(string uploaderId) {
            var url = UploaderUrlPattern.Format(new Dictionary<string, string> {{"id", uploaderId}, {"page", "1"}});
            var result = client.GetAsync(url).Result.GetObject<UploaderResponse>();
            var allResult = result.Clone();
            var page = 1;
            while (result.Data?.Page?.Count > allResult.Data?.List?.Vlist?.Count) {
                url = UploaderUrlPattern.Format(new Dictionary<string, string> {
                    {"id", uploaderId}, {"page", (++page).ToString()}
                });
                Thread.Sleep(TimeSpan.FromSeconds(1));
                result = client.GetAsync(url).Result.GetObject<UploaderResponse>();
                allResult.Data.List.Vlist.AddRange(result.Data.List.Vlist);
            }

            return allResult;
        }
    }
}
