using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using Pimix;

namespace Kifa.Bilibili.BilibiliApi {
    public class MediaSeasonRpc : JsonRpc<string, MediaSeasonRpc.MediaSeasonResponse> {
        public class MediaSeasonResponse {
            public long Code { get; set; }
            public string Message { get; set; }
            public Result Result { get; set; }
        }

        public class Result {
            public SectionInfo MainSection { get; set; }
            public List<SectionInfo> Section { get; set; }
        }

        public class SectionInfo {
            public List<SectionEpisode> Episodes { get; set; }
            public long Id { get; set; }
            public string Title { get; set; }
            public long Type { get; set; }
        }

        public class SectionEpisode {
            public long Aid { get; set; }
            public string Badge { get; set; }
            public BadgeInfo BadgeInfo { get; set; }
            public long BadgeType { get; set; }
            public long Cid { get; set; }
            public Uri Cover { get; set; }
            public string From { get; set; }
            public long Id { get; set; }
            public long IsPremiere { get; set; }
            public string LongTitle { get; set; }
            public Uri ShareUrl { get; set; }
            public long Status { get; set; }
            public string Title { get; set; }
            public string Vid { get; set; }
        }

        public class BadgeInfo {
            public Color BgColor { get; set; }
            public Color BgColorNight { get; set; }
            public string Text { get; set; }
        }

        public override string UrlPattern { get; } = "https://api.bilibili.com/pgc/web/season/section?season_id={id}";

        public override HttpClient HttpClient { get; } = BilibiliVideo.GetBilibiliClient();

        public override MediaSeasonResponse Call(string seasonId) =>
            Call(new Dictionary<string, string> {{"id", seasonId.Substring(2)}});
    }
}
