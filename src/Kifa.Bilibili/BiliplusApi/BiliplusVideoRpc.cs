using System;
using System.Collections.Generic;

namespace Kifa.Bilibili.BiliplusApi;

public class BiliplusVideoRpc : BiliplusRpc<BiliplusVideoRpc.BiliplusVideoResponse> {
    public class BiliplusVideoResponse {
        public long Id { get; set; }
        public long Ver { get; set; }
        public long Aid { get; set; }
        public string Lastupdate { get; set; }
        public long Lastupdatets { get; set; }
        public string? Title { get; set; }
        public string Description { get; set; }
        public Uri Pic { get; set; }
        public long Tid { get; set; }
        public string Typename { get; set; }
        public long Created { get; set; }
        public string CreatedAt { get; set; }
        public string Author { get; set; }
        public long Mid { get; set; }
        public long Play { get; set; }
        public long Coins { get; set; }
        public long Review { get; set; }
        public long VideoReview { get; set; }
        public long Favorites { get; set; }
        public string Tag { get; set; }
        public List<List> List { get; set; }
        public V2AppApi? V2AppApi { get; set; }
    }

    public class List {
        public int Page { get; set; }
        public string Type { get; set; }
        public long Cid { get; set; }
        public string Vid { get; set; }
        public string Part { get; set; }
    }

    public class V2AppApi {
        public long Aid { get; set; }
        public string Bvid { get; set; }
        public long Cid { get; set; }
        public CmConfig CmConfig { get; set; }
        public Config Config { get; set; }
        public long Copyright { get; set; }
        public long Ctime { get; set; }
        public string Desc { get; set; }
        public Dimension? Dimension { get; set; }
        public long DmSeg { get; set; }
        public long Duration { get; set; }
        public string Dynamic { get; set; }
        public Owner Owner { get; set; }
        public OwnerExt OwnerExt { get; set; }
        public List<PageInfo> Pages { get; set; }
        public Paster Paster { get; set; }
        public Uri Pic { get; set; }
        public long PlayParam { get; set; }
        public long Pubdate { get; set; }
        public Dictionary<string, long> Rights { get; set; }
        public string ShareSubtitle { get; set; }
        public Uri ShortLink { get; set; }
        public Stat Stat { get; set; }
        public long State { get; set; }
        public TIcon TIcon { get; set; }
        public List<Tag>? Tag { get; set; }
        public long Tid { get; set; }
        public string Title { get; set; }
        public string Tname { get; set; }
        public long Videos { get; set; }
    }

    public class CmConfig {
        public AdsControl AdsControl { get; set; }
    }

    public class AdsControl {
        public long HasDanmu { get; set; }
    }

    public class Config {
        public string RelatesTitle { get; set; }
        public long ShareStyle { get; set; }
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

    public class OwnerExt {
        public OfficialVerify OfficialVerify { get; set; }
        public Vip Vip { get; set; }
        public object Assists { get; set; }
        public long Fans { get; set; }
        public string ArcCount { get; set; }
    }

    public class OfficialVerify {
        public long Type { get; set; }
        public string Desc { get; set; }
    }

    public class Vip {
        public long VipType { get; set; }
        public long VipDueDate { get; set; }
        public string DueRemark { get; set; }
        public long AccessStatus { get; set; }
        public long VipStatus { get; set; }
        public string VipStatusWarn { get; set; }
        public long ThemeType { get; set; }
        public Label Label { get; set; }
    }

    public class Label {
        public string Path { get; set; }
        public string Text { get; set; }
        public string LabelTheme { get; set; }
    }

    public class PageInfo {
        public long Cid { get; set; }
        public int Page { get; set; }
        public string From { get; set; }
        public string Part { get; set; }
        public long Duration { get; set; }
        public string Vid { get; set; }
        public string Weblink { get; set; }
        public Dimension Dimension { get; set; }
        public Uri Dmlink { get; set; }
        public string DownloadTitle { get; set; }
        public string DownloadSubtitle { get; set; }
    }

    public class Paster {
        public long Aid { get; set; }
        public long Cid { get; set; }
        public long Duration { get; set; }
        public long Type { get; set; }
        public long AllowJump { get; set; }
        public string Url { get; set; }
    }

    public class TIcon {
        public Act Act { get; set; }
        public Act New { get; set; }
    }

    public class Act {
        public Uri Icon { get; set; }
    }

    public class Tag {
        public long TagId { get; set; }
        public string TagName { get; set; }
        public string Cover { get; set; }
        public long Likes { get; set; }
        public long Hates { get; set; }
        public long Liked { get; set; }
        public long Hated { get; set; }
        public long Attribute { get; set; }
        public long IsActivity { get; set; }
        public string Uri { get; set; }
        public string TagType { get; set; }
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
    }

    public override string UrlPattern { get; } = "https://www.biliplus.com/api/view?id={aid}";

    public BiliplusVideoResponse? Call(string aid)
        => Call(new Dictionary<string, string> {
            { "aid", aid.Substring(2) }
        });
}
