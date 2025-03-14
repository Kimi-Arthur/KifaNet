using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using Kifa.Rpc;
using Kifa.Service;

namespace Kifa.Bilibili.BiliplusApi;

public class BiliplusVideoCacheRpc : KifaJsonParameterizedRpc<BiliplusVideoCacheRpc.Response> {
    #region BiliplusVideoCacheRpc.Response

    public class Response {
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

    #endregion

    protected override string Url => "https://www.biliplus.com{api_path}";

    protected override HttpMethod Method => HttpMethod.Get;

    const string CachePagePattern = "https://www.biliplus.com/all/video/{aid}/";

    static readonly Regex ApiRegex = new(@".'(/api/view_all.*)'.*");

    public BiliplusVideoCacheRpc(string aid) {
        var url = CachePagePattern.Format(("aid", aid));
        var match = ApiRegex.Match(HttpClients.BiliplusHttpClient.GetAsync(url).Result.GetString());
        if (!match.Success) {
            throw new DataNotFoundException($"Failed to find cache page url for {aid}.");
        }

        Parameters = new() {
            { "api_path", match.Groups[1].Value }
        };
    }
}
