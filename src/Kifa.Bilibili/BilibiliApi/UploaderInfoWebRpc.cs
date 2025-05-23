using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;
using Newtonsoft.Json;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class
    UploaderInfoWebRpc : KifaParameterizedRpc, KifaRpc<UploaderInfoWebRpc.Response> {
    #region Response

    public class Response {
        public Common Common { get; set; }
        public Home Home { get; set; }
        public Channel Channel { get; set; }
        public Video Video { get; set; }
        public Search Search { get; set; }
        public Space Space { get; set; }
        public Opus Opus { get; set; }
        public Tag Tag { get; set; }
        public Topic Topic { get; set; }
        public Playlist Playlist { get; set; }
        public Tribee Tribee { get; set; }
        public Route Route { get; set; }
    }

    public class Channel {
        public object[] ChannelConfig { get; set; }
        public ChannelData ChannelData { get; set; }
        public object ServerFetched { get; set; }
    }

    public class ChannelData {
    }

    public class Common {
        public UserInfo UserInfo { get; set; }
        public ServerConfig ServerConfig { get; set; }
        public Abtest Abtest { get; set; }
        public string Ua { get; set; }
        public Dictionary<string, bool> Browser { get; set; }
        public string DefDomain { get; set; }
        public bool IsWxTagLaunch { get; set; }
        public bool NoCallApp { get; set; }
        public Switch Switch { get; set; }
    }

    public class Abtest {
        public string SearchHomepageAwake { get; set; }
        public long H5LabordayStyle { get; set; }
    }

    public class ServerConfig {
        public Constants Constants { get; set; }
        public LimitChannel LimitChannel { get; set; }
        public CustomChannel CustomChannel { get; set; }
        public Switch Switch { get; set; }
    }

    public class Constants {
        public bool NewClipboard { get; set; }
    }

    public class CustomChannel {
        public string[] Bsource { get; set; }
    }

    public class LimitChannel {
        public string[] Ua { get; set; }
        public string[] Bsource { get; set; }
        public string[] Referrer { get; set; }
    }

    public class Switch {
        public string SearchH5Style { get; set; }
    }

    public class UserInfo {
        public bool IsLogin { get; set; }
        public string Face { get; set; }
        public long VipStatus { get; set; }
        public long VipType { get; set; }
    }

    public class Home {
        public HotList HotList { get; set; }
    }

    public class HotList {
        public object[] Result { get; set; }
        public ChannelData Extra { get; set; }
        public bool Error { get; set; }
        public bool? NoMore { get; set; }
        public long? Total { get; set; }
        public bool? HotListNoMore { get; set; }
    }

    public class Opus {
        public string Id { get; set; }
        public object Detail { get; set; }
        public object Preview { get; set; }
        public bool IsClient { get; set; }
        public Fallback Fallback { get; set; }
        public object VerifyData { get; set; }
        public bool IsTab3 { get; set; }
        public string UniqueK { get; set; }
    }

    public class Fallback {
        public string Id { get; set; }
        public long Type { get; set; }
    }

    public class Playlist {
        public string MediaId { get; set; }
        public long Ps { get; set; }
        public long Tid { get; set; }
        public long PlaylistType { get; set; }
        public long P { get; set; }
        public bool IsClient { get; set; }
        public bool HasMore { get; set; }
        public MediaData MediaData { get; set; }
        public object[] MediaList { get; set; }
        public object[] RandomList { get; set; }
        public long VideoIndex { get; set; }
        public ChannelData VideoInfo { get; set; }
        public string PlayMode { get; set; }
        public object[] PlayUrl { get; set; }
        public bool ForbidPreview { get; set; }
        public bool PlayEnd { get; set; }
        public long PlaySum { get; set; }
    }

    public class MediaData {
        public object[] MediaList { get; set; }
        public long Ps { get; set; }
        public long Pn { get; set; }
        public bool NoMore { get; set; }
    }

    public class Route {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Hash { get; set; }
        public ChannelData Query { get; set; }
        public Params Params { get; set; }
        public string FullPath { get; set; }
        public ChannelData Meta { get; set; }
        public From From { get; set; }
    }

    public class From {
        public object Name { get; set; }
        public string Path { get; set; }
        public string Hash { get; set; }
        public ChannelData Query { get; set; }
        public ChannelData Params { get; set; }
        public string FullPath { get; set; }
        public ChannelData Meta { get; set; }
    }

    public class Params {
        public long Id { get; set; }
    }

    public class Search {
        public SearchAllResult SearchAllResult { get; set; }
        public SearchBangumiResult SearchBangumiResult { get; set; }
        public SearchBangumiResult SearchUserResult { get; set; }
        public SearchBangumiResult SearchFilmResult { get; set; }
        public bool OpenAppDialog { get; set; }
        public string Keyword { get; set; }
        public bool IsCustom { get; set; }
        public string Bsource { get; set; }
    }

    public class SearchAllResult {
        public Extra Extra { get; set; }
        public SearchBangumiResult Totalrank { get; set; }
        public SearchBangumiResult Click { get; set; }
        public SearchBangumiResult Pubdate { get; set; }
        public SearchBangumiResult Dm { get; set; }
    }

    public class SearchBangumiResult {
        public object[] Result { get; set; }
        public bool NoMore { get; set; }
        public long Page { get; set; }
        public long Total { get; set; }
    }

    public class Extra {
        public Count Count { get; set; }
        public object[] PgcList { get; set; }
        public object[] FilmList { get; set; }
    }

    public class Count {
        public long Video { get; set; }
        public long MediaBangumi { get; set; }
        public long BiliUser { get; set; }
        public long MediaFt { get; set; }
    }

    public class Space {
        public long Mid { get; set; }
        public Info Info { get; set; }
        public HotList FeedList { get; set; }
    }

    public class Info {
        public long Mid { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; }
        public string Face { get; set; }
        public long FaceNft { get; set; }
        public long FaceNftType { get; set; }
        public string Sign { get; set; }
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
        public string TopPhoto { get; set; }
        public ChannelData SysNotice { get; set; }
        public LiveRoom LiveRoom { get; set; }
        public string Birthday { get; set; }
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
        public object Contract { get; set; }
        public bool CertificateShow { get; set; }
        public object NameRender { get; set; }
        public TopPhotoV2 TopPhotoV2 { get; set; }
        public object Theme { get; set; }
        public Attestation Attestation { get; set; }
    }

    public class Attestation {
        public long Type { get; set; }
        public CommonInfo CommonInfo { get; set; }
        public SpliceInfo SpliceInfo { get; set; }
        public string Icon { get; set; }
        public string Desc { get; set; }
    }

    public class CommonInfo {
        public string Title { get; set; }
        public string Prefix { get; set; }
        public string PrefixTitle { get; set; }
    }

    public class SpliceInfo {
        public string Title { get; set; }
    }

    public class Elec {
        public ShowInfo ShowInfo { get; set; }
    }

    public class ShowInfo {
        public bool Show { get; set; }
        public long State { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
        public string JumpUrl { get; set; }
    }

    public class FansMedal {
        public bool Show { get; set; }
        public bool Wear { get; set; }
        public object Medal { get; set; }
    }

    public class LiveRoom {
        public long RoomStatus { get; set; }
        public long LiveStatus { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Cover { get; set; }
        public long Roomid { get; set; }
        public long RoundStatus { get; set; }
        public long BroadcastType { get; set; }
        public WatchedShow WatchedShow { get; set; }
    }

    public class WatchedShow {
        public bool Switch { get; set; }
        public long Num { get; set; }
        public long TextSmall { get; set; }
        public string TextLarge { get; set; }
        public string Icon { get; set; }
        public string IconLocation { get; set; }
        public string IconWeb { get; set; }
    }

    public class Nameplate {
        public long Nid { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string ImageSmall { get; set; }
        public string Level { get; set; }
        public string Condition { get; set; }
    }

    public class Official {
        public long Role { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }
        public long Type { get; set; }
    }

    public class Pendant {
        public long Pid { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public long Expire { get; set; }
        public string ImageEnhance { get; set; }
        public string ImageEnhanceFrame { get; set; }
        public long NPid { get; set; }
    }

    public class Profession {
        public string Name { get; set; }
        public string Department { get; set; }
        public string Title { get; set; }
        public long IsShow { get; set; }
    }

    public class Series {
        public long UserUpgradeStatus { get; set; }
        public bool ShowUpgradeWindow { get; set; }
    }

    public class TopPhotoV2 {
        public long Sid { get; set; }
        public string LImg { get; set; }
        public string L200HImg { get; set; }
    }

    public class UserHonourInfo {
        public long Mid { get; set; }
        public object Colour { get; set; }
        public object[] Tags { get; set; }
        public long IsLatest100Honour { get; set; }
    }

    public class Vip {
        public long Type { get; set; }
        public long Status { get; set; }
        public long DueDate { get; set; }
        public long VipPayType { get; set; }
        public long ThemeType { get; set; }
        public Label Label { get; set; }
        public long AvatarSubscript { get; set; }
        public string NicknameColor { get; set; }
        public long Role { get; set; }
        public string AvatarSubscriptUrl { get; set; }
        public long TvVipStatus { get; set; }
        public long TvVipPayType { get; set; }
        public long TvDueDate { get; set; }
        public AvatarIcon AvatarIcon { get; set; }
    }

    public class AvatarIcon {
        public long IconType { get; set; }
        public ChannelData IconResource { get; set; }
    }

    public class Label {
        public string Path { get; set; }
        public string Text { get; set; }
        public string LabelTheme { get; set; }
        public string TextColor { get; set; }
        public long BgStyle { get; set; }
        public string BgColor { get; set; }
        public string BorderColor { get; set; }
        public bool UseImgLabel { get; set; }
        public string ImgLabelUriHans { get; set; }
        public string ImgLabelUriHant { get; set; }
        public string ImgLabelUriHansStatic { get; set; }
        public string ImgLabelUriHantStatic { get; set; }
    }

    public class Tag {
        public ChannelData TagInfo { get; set; }
        public object[] TagSimilar { get; set; }
        public HotList TagRelated { get; set; }
    }

    public class Topic {
        public ChannelData TopicDetail { get; set; }
        public ChannelData Activities { get; set; }
        public ChannelData Ads { get; set; }
        public long TopicId { get; set; }
        public HotList TopicListResult { get; set; }
        public object[] RelevantTopics { get; set; }
        public long HeaderTextColor { get; set; }
        public bool IsExpanded { get; set; }
    }

    public class Tribee {
        public ChannelData TribeeBaseInfo { get; set; }
        public object[] TribeeList { get; set; }
        public long TribeeListTotal { get; set; }
    }

    public class Video {
        public long Error { get; set; }
        public bool IsClient { get; set; }
        public ChannelData Breadcrumb { get; set; }
        public ChannelData ViewInfo { get; set; }
        public ChannelData UpInfo { get; set; }
        public HotList Related { get; set; }
        public object[] Tags { get; set; }
        public ChannelData Elec { get; set; }
        public long P { get; set; }
        public long Avid { get; set; }
        public string Bvid { get; set; }
        public ChannelData Quality { get; set; }
        public ChannelData PlayUrlInfo { get; set; }
        public string PlayState { get; set; }
        public string Bsource { get; set; }
        public bool GameMode { get; set; }
        public string Keyword { get; set; }
        public bool TogglePlay { get; set; }
        public bool IsTab3 { get; set; }
        public bool IsCustom { get; set; }
        public bool IsSem { get; set; }
        public ChannelData ReportMsg { get; set; }
        public string PageType { get; set; }
        public ChannelData PlayerSettings { get; set; }
        public bool IsRelatedUp { get; set; }
        public bool IsHitLabourDayActivity { get; set; }
        public bool IsHitLabourDayAbTest { get; set; }
    }

    #endregion

    protected override string Url => "https://m.bilibili.com/space/{id}";

    protected override HttpMethod Method => HttpMethod.Get;

    protected override Dictionary<string, string> Headers
        => new() {
            {
                "user-agent",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 16_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Mobile/15E148 Safari/604.1"
            }
        };

    public UploaderInfoWebRpc(string uploaderId) {
        Parameters = new() {
            { "id", uploaderId }
        };
    }

    const string JsonPrefix = "__INITIAL_STATE__=";
    const string JsonSuffix = ";(";

    public Response? ParseResponse(HttpResponseMessage responseMessage) {
        var html = responseMessage.GetString();
        html = html[(html.IndexOf(JsonPrefix) + JsonPrefix.Length)..];
        var json = html[..html.IndexOf(JsonSuffix)];
        return JsonConvert.DeserializeObject<Response>(json, KifaJsonSerializerSettings.Default);
    }
}
