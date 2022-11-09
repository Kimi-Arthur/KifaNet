using System;
using System.Collections.Generic;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class BilibiliMangaRequest : ParameterizedRequest {
    public override string UrlPattern
        => "https://manga.bilibili.com/twirp/comic.v1.Comic/ComicDetail?device=pc&platform=web";

    public override string JsonContent => "{\"comic_id\":{comic_id}}";

    public BilibiliMangaRequest(string mangaId) {
        parameters = new Dictionary<string, string> {
            { "comic_id", mangaId }
        };
    }
}

public class BilibiliMangaResponse {
    public long Code { get; set; }
    public string Msg { get; set; }
    public BilibiliMangaData Data { get; set; }
}

public class BilibiliMangaData {
    public long Id { get; set; }
    public string Title { get; set; }
    public long ComicType { get; set; }
    public long PageDefault { get; set; }
    public long PageAllow { get; set; }
    public string HorizontalCover { get; set; }
    public string SquareCover { get; set; }
    public string VerticalCover { get; set; }
    public List<string> AuthorName { get; set; }
    public List<string> Styles { get; set; }
    public long LastOrd { get; set; }
    public long IsFinish { get; set; }
    public long Status { get; set; }
    public long Fav { get; set; }
    public long ReadOrder { get; set; }
    public string Evaluate { get; set; }
    public long Total { get; set; }
    public EpisodeInfo[] EpList { get; set; }
    public string ReleaseTime { get; set; }
    public long IsLimit { get; set; }
    public long ReadEpid { get; set; }
    public DateTime LastReadTime { get; set; }
    public long IsDownload { get; set; }
    public long ReadShortTitle { get; set; }
    public Styles2[] Styles2 { get; set; }
    public string RenewalTime { get; set; }
    public long LastShortTitle { get; set; }
    public long DiscountType { get; set; }
    public long Discount { get; set; }
    public DateTime DiscountEnd { get; set; }
    public bool NoReward { get; set; }
    public long BatchDiscountType { get; set; }
    public long EpDiscountType { get; set; }
    public bool HasFavActivity { get; set; }
    public long FavFreeAmount { get; set; }
    public bool AllowWaitFree { get; set; }
    public long WaitHour { get; set; }
    public long NoDanmaku { get; set; }
    public long AutoPayStatus { get; set; }
    public bool NoMonthTicket { get; set; }
    public bool Immersive { get; set; }
    public bool NoDiscount { get; set; }
    public long ShowType { get; set; }
    public long PayMode { get; set; }
    public object[] Chapters { get; set; }
    public string ClassicLines { get; set; }
    public long PayForNew { get; set; }
    public long SerialStatus { get; set; }
    public long AlbumCount { get; set; }
    public long WikiId { get; set; }
    public long DisableCouponAmount { get; set; }
    public bool JapanComic { get; set; }
    public long InteractValue { get; set; }
    public string TemporaryFinishTime { get; set; }
    public object Video { get; set; }
    public string Introduction { get; set; }
    public long CommentStatus { get; set; }
    public bool NoScreenshot { get; set; }
    public long Type { get; set; }
    public object[] VomicCvs { get; set; }
    public bool NoRank { get; set; }
    public object[] PresaleEps { get; set; }
    public string PresaleText { get; set; }
    public long PresaleDiscount { get; set; }
    public bool NoLeaderboard { get; set; }
    public AutoPayInfo AutoPayInfo { get; set; }
}

public class AutoPayInfo {
    public AutoPayOrder[] AutoPayOrders { get; set; }
    public long Id { get; set; }
}

public class AutoPayOrder {
    public long Id { get; set; }
    public string Title { get; set; }
}

public class EpisodeInfo {
    public long Id { get; set; }
    public long Ord { get; set; }
    public long Read { get; set; }
    public long PayMode { get; set; }
    public bool IsLocked { get; set; }
    public long PayGold { get; set; }
    public long Size { get; set; }
    public string ShortTitle { get; set; }
    public bool IsInFree { get; set; }
    public string Title { get; set; }
    public string Cover { get; set; }
    public DateTime PubTime { get; set; }
    public long Comments { get; set; }
    public long UnlockType { get; set; }
    public bool AllowWaitFree { get; set; }
    public string Progress { get; set; }
    public long LikeCount { get; set; }
    public long ChapterId { get; set; }
    public long Type { get; set; }
    public long Extra { get; set; }
    public long ImageCount { get; set; }
}

public class Styles2 {
    public long Id { get; set; }
    public string Name { get; set; }
}
