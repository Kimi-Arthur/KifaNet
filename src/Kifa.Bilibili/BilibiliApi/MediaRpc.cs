using System;
using System.Collections.Generic;

namespace Kifa.Bilibili.BilibiliApi {
    public class MediaRpc : BilibiliRpc<MediaRpc.MediaResponse> {
        public class MediaResponse {
            public long Code { get; set; }
            public string Message { get; set; }
            public Result Result { get; set; }
        }

        public class Result {
            public Media Media { get; set; }
        }

        public class Media {
            public List<Area> Areas { get; set; }
            public Uri Cover { get; set; }
            public long MediaId { get; set; }
            public NewEp NewEp { get; set; }
            public Rating Rating { get; set; }
            public long SeasonId { get; set; }
            public Uri ShareUrl { get; set; }
            public string Title { get; set; }
            public string TypeName { get; set; }
        }

        public class Area {
            public long Id { get; set; }
            public string Name { get; set; }
        }

        public class NewEp {
            public long Id { get; set; }
            public string Index { get; set; }
            public string IndexShow { get; set; }
        }

        public class Rating {
            public long Count { get; set; }
            public double Score { get; set; }
        }

        public override string UrlPattern { get; } = "https://api.bilibili.com/pgc/review/user?media_id={id}";

        public MediaResponse Call(string mediaId) =>
            Call(new Dictionary<string, string> {{"id", mediaId.Substring(2)}});
    }
}
