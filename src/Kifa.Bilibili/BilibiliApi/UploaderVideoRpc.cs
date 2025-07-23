using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class UploaderVideoRpc : KifaJsonParameterizedRpc<UploaderVideoRpc.Response> {
    #region UploaderVideoRpc.Response

    public class Response {
        public long Code { get; set; }
        public string Msg { get; set; }
        public string Message { get; set; }
        public Data Data { get; set; }
    }

    public class Data {
        public long HasMore { get; set; }
        public Attentions Attentions { get; set; }
        public List<CardElement> Cards { get; set; } = [];
        public long NextOffset { get; set; }
        public long Gt { get; set; }
    }

    public class Attentions {
        public List<long> Uids { get; set; }
    }

    public class CardElement {
        public Desc Desc { get; set; }
        public string Card { get; set; }
        public string ExtendJson { get; set; }
        public Extra Extra { get; set; }
        public Display Display { get; set; }
        public Extension Extension { get; set; }
    }

    public class Desc {
        public long Uid { get; set; }
        public long Type { get; set; }
        public long Rid { get; set; }
        public long Acl { get; set; }
        public long View { get; set; }
        public long Repost { get; set; }
        public long? Comment { get; set; }
        public long Like { get; set; }
        public long IsLiked { get; set; }
        public long DynamicId { get; set; }
        public long Timestamp { get; set; }
        public long PreDyId { get; set; }
        public long OrigDyId { get; set; }
        public long? OrigType { get; set; }
        public UserProfile UserProfile { get; set; }
        public long UidType { get; set; }
        public long Stype { get; set; }
        public long RType { get; set; }
        public long InnerId { get; set; }
        public long Status { get; set; }
        public string DynamicIdStr { get; set; }
        public string PreDyIdStr { get; set; }
        public string OrigDyIdStr { get; set; }
        public string RidStr { get; set; }
        public Desc Origin { get; set; }
        public string Bvid { get; set; }
    }

    public class UserProfile {
        public Info Info { get; set; }
        public UserProfileCard Card { get; set; }
        public Vip Vip { get; set; }
        public Pendant Pendant { get; set; }
        public string Rank { get; set; }
        public string Sign { get; set; }
        public LevelInfo LevelInfo { get; set; }
    }

    public class UserProfileCard {
        public OfficialVerify OfficialVerify { get; set; }
    }

    public class OfficialVerify {
        public long Type { get; set; }
        public string Desc { get; set; }
    }

    public class Info {
        public long Uid { get; set; }
        public string Uname { get; set; }
        public string Face { get; set; }
        public long FaceNft { get; set; }
    }

    public class LevelInfo {
        public long CurrentLevel { get; set; }
    }

    public class Pendant {
        public long Pid { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public long Expire { get; set; }
        public string ImageEnhance { get; set; }
        public string ImageEnhanceFrame { get; set; }
    }

    public class Vip {
        public long VipType { get; set; }
        public long VipDueDate { get; set; }
        public long VipStatus { get; set; }
        public long ThemeType { get; set; }
        public Label Label { get; set; }
        public long AvatarSubscript { get; set; }
        public string NicknameColor { get; set; }
        public long Role { get; set; }
        public string AvatarSubscriptUrl { get; set; }
    }

    public class Label {
        public string Path { get; set; }
        public string Text { get; set; }
        public string LabelTheme { get; set; }
        public string TextColor { get; set; }
        public long BgStyle { get; set; }
        public string BgColor { get; set; }
        public string BorderColor { get; set; }
    }

    public class Display {
        public Display Origin { get; set; }
        public Relation Relation { get; set; }
        public string UsrActionTxt { get; set; }
        public EmojiInfo EmojiInfo { get; set; }
    }

    public class EmojiInfo {
        public List<EmojiDetail> EmojiDetails { get; set; }
    }

    public class EmojiDetail {
        public string EmojiName { get; set; }
        public long Id { get; set; }
        public long PackageId { get; set; }
        public long State { get; set; }
        public long Type { get; set; }
        public long Attr { get; set; }
        public string Text { get; set; }
        public string Url { get; set; }
        public Meta Meta { get; set; }
        public long Mtime { get; set; }
    }

    public class Meta {
        public long Size { get; set; }
    }

    public class Relation {
        public long Status { get; set; }
        public long IsFollow { get; set; }
        public long IsFollowed { get; set; }
    }

    public class Extension {
        public string Lott { get; set; }
    }

    public class Extra {
        public long IsSpaceTop { get; set; }
    }

    #endregion

    protected override string Url
        => "https://api.vc.bilibili.com/dynamic_svr/v1/dynamic_svr/space_history?host_uid={id}&offset_dynamic_id={offset}";

    protected override HttpMethod Method => HttpMethod.Get;

    public UploaderVideoRpc(string uploaderId, long offset = 0) {
        Parameters = new() {
            { "id", uploaderId },
            { "offset", offset.ToString() }
        };
    }
}
