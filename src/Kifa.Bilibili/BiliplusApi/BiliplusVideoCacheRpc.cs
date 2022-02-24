using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Kifa.Bilibili.BiliplusApi;

public class BiliplusVideoCacheRpc : BiliplusRpc<BiliplusVideoCacheRpc.BiliplusVideoCache> {
    public class BiliplusVideoCache {
        public long Code { get; set; }
        public Data Data { get; set; }
    }

    public class Data {
        public long Id { get; set; }
        public Info Info { get; set; }
        public List<PartType> Parts { get; set; }
    }

    public class Info {
        public bool IsDetailed { get; set; }
        public string Title { get; set; }
        public string Typename { get; set; }
        public long Play { get; set; }
        public long Review { get; set; }
        public long VideoReview { get; set; }
        public long Favorites { get; set; }
        public long Coins { get; set; }
        public string Keywords { get; set; }
        public string Description { get; set; }
        public string Create { get; set; }
        public string Author { get; set; }
        public long Mid { get; set; }
        public Uri Pic { get; set; }
        public long Aid { get; set; }
    }

    public class PartType {
        public int Page { get; set; }
        public string Part { get; set; }
        public long Cid { get; set; }
        public string Type { get; set; }
        public string Vid { get; set; }
    }

    const string CachePagePattern = "https://www.biliplus.com/all/video/{aid}/";

    static readonly Regex ApiRegex = new(@".'(/api/view_all.*)'.*");

    public override string UrlPattern { get; } = "https://www.biliplus.com{api_path}";

    public BiliplusVideoCache Call(string aid) {
        var url = CachePagePattern.Format(new Dictionary<string, string> {
            { "aid", aid }
        });
        var match = ApiRegex.Match(HttpClient.GetAsync(url).Result.GetString());
        return match.Success
            ? Call(new Dictionary<string, string> {
                { "api_path", match.Groups[1].Value }
            })
            : null;
    }
}
