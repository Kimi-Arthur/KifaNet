using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class MediaSeasonRpc : KifaJsonParameterizedRpc<MediaSeasonResponse> {
    protected override string Url
        => "https://api.bilibili.com/pgc/web/season/section?season_id={id}";

    protected override HttpMethod Method => HttpMethod.Get;

    public MediaSeasonRpc(string seasonId) {
        Parameters = new Dictionary<string, string> {
            { "id", seasonId[2..] }
        };
    }
}

public class MediaSeasonResponse {
    public long Code { get; set; }
    public string Message { get; set; }
    public MediaSeasonResult Result { get; set; }
}

public class MediaSeasonResult {
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
