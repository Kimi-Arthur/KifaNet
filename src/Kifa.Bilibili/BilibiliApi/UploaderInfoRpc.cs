using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class UploaderInfoRpc : KifaJsonParameterizedRpc<UploaderInfoRpc.Response> {
    #region UploadInfoRpc.Response

    public class Response {
        public long Code { get; set; }
        public string? Message { get; set; }
        public long Ttl { get; set; }
        public Data Data { get; set; }
    }

    public class Data {
        public long Mid { get; set; }
        public string? Name { get; set; }
        public string? Sex { get; set; }
        public string? Face { get; set; }
        public long FaceNft { get; set; }
        public long FaceNftType { get; set; }
        public string? Sign { get; set; }
        public long Rank { get; set; }
        public long Level { get; set; }
        public long Jointime { get; set; }
        public long Moral { get; set; }
        public long Silence { get; set; }
        public long Coins { get; set; }
        public bool FansBadge { get; set; }
        public FansMedal FansMedal { get; set; }
        public Official Official { get; set; }
        public Vip Vip { get; set; }
        public Pendant Pendant { get; set; }
        public Nameplate Nameplate { get; set; }
        public UserHonourInfo UserHonourInfo { get; set; }
        public bool IsFollowed { get; set; }
        public string? TopPhoto { get; set; }
        public SysNotice Theme { get; set; }
        public SysNotice SysNotice { get; set; }
        public LiveRoom LiveRoom { get; set; }
        public string? Birthday { get; set; }
        public object School { get; set; }
        public Profession Profession { get; set; }
        public object Tags { get; set; }
        public Series Series { get; set; }
        public long IsSeniorMember { get; set; }
        public object McnInfo { get; set; }
        public long GaiaResType { get; set; }
        public object GaiaData { get; set; }
        public bool IsRisk { get; set; }
        public Elec Elec { get; set; }
        public Contract Contract { get; set; }
        public bool CertificateShow { get; set; }
    }

    public class Contract {
        public bool IsDisplay { get; set; }
        public bool IsFollowDisplay { get; set; }
    }

    public class Elec {
        public ShowInfo ShowInfo { get; set; }
    }

    public class ShowInfo {
        public bool Show { get; set; }
        public long State { get; set; }
        public string? Title { get; set; }
        public string? Icon { get; set; }
        public string? JumpUrl { get; set; }
    }

    public class FansMedal {
        public bool Show { get; set; }
        public bool Wear { get; set; }
        public object Medal { get; set; }
    }

    public class LiveRoom {
        public long RoomStatus { get; set; }
        public long LiveStatus { get; set; }
        public string? Url { get; set; }
        public string? Title { get; set; }
        public string? Cover { get; set; }
        public long Roomid { get; set; }
        public long RoundStatus { get; set; }
        public long BroadcastType { get; set; }
        public WatchedShow WatchedShow { get; set; }
    }

    public class WatchedShow {
        public bool Switch { get; set; }
        public long Num { get; set; }
        public string? TextSmall { get; set; }
        public string? TextLarge { get; set; }
        public string? Icon { get; set; }
        public string? IconLocation { get; set; }
        public string? IconWeb { get; set; }
    }

    public class Nameplate {
        public long Nid { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }
        public string? ImageSmall { get; set; }
        public string? Level { get; set; }
        public string? Condition { get; set; }
    }

    public class Official {
        public long Role { get; set; }
        public string? Title { get; set; }
        public string? Desc { get; set; }
        public long Type { get; set; }
    }

    public class Pendant {
        public long Pid { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }
        public long Expire { get; set; }
        public string? ImageEnhance { get; set; }
        public string? ImageEnhanceFrame { get; set; }
    }

    public class Profession {
        public string? Name { get; set; }
        public string? Department { get; set; }
        public string? Title { get; set; }
        public long IsShow { get; set; }
    }

    public class Series {
        public long UserUpgradeStatus { get; set; }
        public bool ShowUpgradeWindow { get; set; }
    }

    public class SysNotice {
    }

    public class UserHonourInfo {
        public long Mid { get; set; }
        public object Colour { get; set; }
        public List<object> Tags { get; set; }
    }

    public class Vip {
        public long Type { get; set; }
        public long Status { get; set; }
        public long DueDate { get; set; }
        public long VipPayType { get; set; }
        public long ThemeType { get; set; }
        public Label Label { get; set; }
        public long AvatarSubscript { get; set; }
        public string? NicknameColor { get; set; }
        public long Role { get; set; }
        public string? AvatarSubscriptUrl { get; set; }
        public long TvVipStatus { get; set; }
        public long TvVipPayType { get; set; }
        public long TvDueDate { get; set; }
    }

    public class Label {
        public string? Path { get; set; }
        public string? Text { get; set; }
        public string? LabelTheme { get; set; }
        public string? TextColor { get; set; }
        public long BgStyle { get; set; }
        public string? BgColor { get; set; }
        public string? BorderColor { get; set; }
        public bool UseImgLabel { get; set; }
        public string? ImgLabelUriHans { get; set; }
        public string? ImgLabelUriHant { get; set; }
        public string? ImgLabelUriHansStatic { get; set; }
        public string? ImgLabelUriHantStatic { get; set; }
    }

    #endregion

    protected override string Url => "https://api.bilibili.com/x/space/wbi/acc/info?mid={id}";

    protected override HttpMethod Method => HttpMethod.Get;

    public UploaderInfoRpc(string uploaderId) {
        // Not working due to new verification method. See discussion in
        // https://github.com/SocialSisterYi/bilibili-API-collect/issues/868
        Parameters = new () {
            { "id", uploaderId }
        };
    }
}
