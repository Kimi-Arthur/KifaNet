using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class UploaderVideoRpc : KifaJsonParameterizedRpc<UploaderVideoRpc.Response> {
    #region UploaderVideoRpc.Response

    public class Response {
        public long Code { get; set; }
        public string Message { get; set; }
        public long Ttl { get; set; }
        public Data Data { get; set; }
    }

    public class Data {
        public bool HasMore { get; set; }
        public List<DataItem> Items { get; set; }
        public string Offset { get; set; }
        public string UpdateBaseline { get; set; }
        public long UpdateNum { get; set; }
    }

    public class DataItem {
        public ItemBasic Basic { get; set; }
        public string IdStr { get; set; }
        public ItemModules Modules { get; set; }
        public string Type { get; set; }
        public bool Visible { get; set; }
        public Orig Orig { get; set; }
    }

    public class ItemBasic {
        public string CommentIdStr { get; set; }
        public long CommentType { get; set; }
        public LikeIcon LikeIcon { get; set; }
        public string RidStr { get; set; }
        public string JumpUrl { get; set; }
    }

    public class LikeIcon {
        public string ActionUrl { get; set; }
        public string EndUrl { get; set; }
        public long Id { get; set; }
        public string StartUrl { get; set; }
    }

    public class ItemModules {
        public PurpleModuleAuthor ModuleAuthor { get; set; }
        public PurpleModuleDynamic ModuleDynamic { get; set; }
        public ModuleInteraction ModuleInteraction { get; set; }
        public ModuleMore ModuleMore { get; set; }
        public ModuleStat ModuleStat { get; set; }
    }

    public class PurpleModuleAuthor {
        public PurpleAvatar Avatar { get; set; }
        public Uri Face { get; set; }
        public bool FaceNft { get; set; }
        public bool Following { get; set; }
        public string JumpUrl { get; set; }
        public string Label { get; set; }
        public long Mid { get; set; }
        public string Name { get; set; }
        public OfficialVerify OfficialVerify { get; set; }
        public PurplePendant Pendant { get; set; }
        public string PubAction { get; set; }
        public string PubLocationText { get; set; }
        public string PubTime { get; set; }
        public long PubTs { get; set; }
        public string Type { get; set; }
        public PurpleVip Vip { get; set; }
    }

    public class PurpleAvatar {
        public ContainerSize ContainerSize { get; set; }
        public PurpleFallbackLayers FallbackLayers { get; set; }
        public string Mid { get; set; }
    }

    public class ContainerSize {
        public double Height { get; set; }
        public double Width { get; set; }
    }

    public class PurpleFallbackLayers {
        public bool IsCriticalGroup { get; set; }
        public List<PurpleLayer> Layers { get; set; }
    }

    public class PurpleLayer {
        public GeneralSpec GeneralSpec { get; set; }
        public PurpleLayerConfig LayerConfig { get; set; }
        public PurpleResource Resource { get; set; }
        public bool Visible { get; set; }
    }

    public class GeneralSpec {
        public PosSpec PosSpec { get; set; }
        public RenderSpec RenderSpec { get; set; }
        public ContainerSize SizeSpec { get; set; }
    }

    public class PosSpec {
        public double AxisX { get; set; }
        public double AxisY { get; set; }
        public long CoordinatePos { get; set; }
    }

    public class RenderSpec {
        public long Opacity { get; set; }
    }

    public class PurpleLayerConfig {
        public bool? IsCritical { get; set; }
        public PurpleTags Tags { get; set; }
    }

    public class PurpleTags {
        public Layer AvatarLayer { get; set; }
        public GeneralCfg GeneralCfg { get; set; }
        public Layer IconLayer { get; set; }
    }

    public class Layer {
    }

    public class GeneralCfg {
        public long ConfigType { get; set; }
        public GeneralConfig GeneralConfig { get; set; }
    }

    public class GeneralConfig {
        public WebCssStyle WebCssStyle { get; set; }
    }

    public class WebCssStyle {
        public string BorderRadius { get; set; }
        public string BackgroundColor { get; set; }
        public string Border { get; set; }
        public string BoxSizing { get; set; }
    }

    public class PurpleResource {
        public PurpleResImage ResImage { get; set; }
        public long ResType { get; set; }
    }

    public class PurpleResImage {
        public PurpleImageSrc ImageSrc { get; set; }
    }

    public class PurpleImageSrc {
        public long? Placeholder { get; set; }
        public PurpleRemote Remote { get; set; }
        public long SrcType { get; set; }
        public long? Local { get; set; }
    }

    public class PurpleRemote {
        public string BfsStyle { get; set; }
        public Uri Url { get; set; }
    }

    public class OfficialVerify {
        public string Desc { get; set; }
        public long Type { get; set; }
    }

    public class PurplePendant {
        public long Expire { get; set; }
        public string Image { get; set; }
        public string ImageEnhance { get; set; }
        public string ImageEnhanceFrame { get; set; }
        public long NPid { get; set; }
        public string Name { get; set; }
        public long Pid { get; set; }
    }

    public class PurpleVip {
        public long AvatarSubscript { get; set; }
        public string AvatarSubscriptUrl { get; set; }
        public long DueDate { get; set; }
        public PurpleLabel Label { get; set; }
        public string NicknameColor { get; set; }
        public long Status { get; set; }
        public long ThemeType { get; set; }
        public long Type { get; set; }
    }

    public class PurpleLabel {
        public string BgColor { get; set; }
        public long BgStyle { get; set; }
        public string BorderColor { get; set; }
        public string ImgLabelUriHans { get; set; }
        public Uri ImgLabelUriHansStatic { get; set; }
        public string ImgLabelUriHant { get; set; }
        public Uri ImgLabelUriHantStatic { get; set; }
        public string LabelTheme { get; set; }
        public Uri Path { get; set; }
        public string Text { get; set; }
        public string TextColor { get; set; }
        public bool UseImgLabel { get; set; }
    }

    public class PurpleModuleDynamic {
        public Additional Additional { get; set; }
        public PurpleDesc Desc { get; set; }
        public PurpleMajor? Major { get; set; }
        public Topic Topic { get; set; }
    }

    public class Additional {
        public Reserve Reserve { get; set; }
        public string Type { get; set; }
    }

    public class Reserve {
        public Button Button { get; set; }
        public Desc1 Desc1 { get; set; }
        public Desc2 Desc2 { get; set; }
        public string JumpUrl { get; set; }
        public long ReserveTotal { get; set; }
        public long Rid { get; set; }
        public long State { get; set; }
        public long Stype { get; set; }
        public string Title { get; set; }
        public long UpMid { get; set; }
    }

    public class Button {
        public Check Check { get; set; }
        public Check JumpStyle { get; set; }
        public string JumpUrl { get; set; }
        public long Status { get; set; }
        public long Type { get; set; }
        public Uncheck Uncheck { get; set; }
    }

    public class Check {
        public string IconUrl { get; set; }
        public string Text { get; set; }
    }

    public class Uncheck {
        public Uri IconUrl { get; set; }
        public string Text { get; set; }
    }

    public class Desc1 {
        public long Style { get; set; }
        public string Text { get; set; }
    }

    public class Desc2 {
        public long Style { get; set; }
        public string Text { get; set; }
        public bool Visible { get; set; }
    }

    public class PurpleDesc {
        public List<PurpleRichTextNode> RichTextNodes { get; set; }
        public string Text { get; set; }
    }

    public class PurpleRichTextNode {
        public string OrigText { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
        public Emoji Emoji { get; set; }
    }

    public class Emoji {
        public Uri IconUrl { get; set; }
        public long Size { get; set; }
        public string Text { get; set; }
        public long Type { get; set; }
    }

    public class PurpleMajor {
        public Archive? Archive { get; set; }
        public string Type { get; set; }
        public Opus Opus { get; set; }
    }

    public class Archive {
        public string Aid { get; set; }
        public Badge Badge { get; set; }
        public string Bvid { get; set; }
        public Uri Cover { get; set; }
        public string Desc { get; set; }
        public long DisablePreview { get; set; }
        public string DurationText { get; set; }
        public string JumpUrl { get; set; }
        public Stat Stat { get; set; }
        public string Title { get; set; }
        public long Type { get; set; }
    }

    public class Badge {
        public string BgColor { get; set; }
        public string Color { get; set; }
        public object IconUrl { get; set; }
        public string Text { get; set; }
    }

    public class Stat {
        public string Danmaku { get; set; }
        public string Play { get; set; }
    }

    public class Opus {
        public List<string> FoldAction { get; set; }
        public string JumpUrl { get; set; }
        public List<Pic> Pics { get; set; }
        public SummaryClass Summary { get; set; }
        public object Title { get; set; }
    }

    public class Pic {
        public long Height { get; set; }
        public object LiveUrl { get; set; }
        public double Size { get; set; }
        public Uri Url { get; set; }
        public long Width { get; set; }
    }

    public class SummaryClass {
        public List<SummaryRichTextNode> RichTextNodes { get; set; }
        public string Text { get; set; }
    }

    public class SummaryRichTextNode {
        public string OrigText { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
    }

    public class Topic {
        public long Id { get; set; }
        public Uri JumpUrl { get; set; }
        public string Name { get; set; }
    }

    public class ModuleInteraction {
        public List<ModuleInteractionItem> Items { get; set; }
    }

    public class ModuleInteractionItem {
        public ItemDesc Desc { get; set; }
        public long Type { get; set; }
    }

    public class ItemDesc {
        public List<FluffyRichTextNode> RichTextNodes { get; set; }
        public string Text { get; set; }
    }

    public class FluffyRichTextNode {
        public string OrigText { get; set; }
        public string Rid { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
    }

    public class ModuleMore {
        public List<ThreePointItem> ThreePointItems { get; set; }
    }

    public class ThreePointItem {
        public string Label { get; set; }
        public string Type { get; set; }
    }

    public class ModuleStat {
        public Comment Comment { get; set; }
        public Comment Forward { get; set; }
        public Like Like { get; set; }
    }

    public class Comment {
        public long Count { get; set; }
        public bool Forbidden { get; set; }
    }

    public class Like {
        public long Count { get; set; }
        public bool Forbidden { get; set; }
        public bool Status { get; set; }
    }

    public class Orig {
        public OrigBasic Basic { get; set; }
        public string IdStr { get; set; }
        public OrigModules Modules { get; set; }
        public string Type { get; set; }
        public bool Visible { get; set; }
    }

    public class OrigBasic {
        public string CommentIdStr { get; set; }
        public long CommentType { get; set; }
        public LikeIcon LikeIcon { get; set; }
        public string RidStr { get; set; }
    }

    public class OrigModules {
        public FluffyModuleAuthor ModuleAuthor { get; set; }
        public FluffyModuleDynamic ModuleDynamic { get; set; }
    }

    public class FluffyModuleAuthor {
        public FluffyAvatar Avatar { get; set; }
        public Uri Face { get; set; }
        public bool FaceNft { get; set; }
        public object Following { get; set; }
        public string JumpUrl { get; set; }
        public string Label { get; set; }
        public long Mid { get; set; }
        public string Name { get; set; }
        public OfficialVerify OfficialVerify { get; set; }
        public FluffyPendant Pendant { get; set; }
        public string PubAction { get; set; }
        public string PubTime { get; set; }
        public long PubTs { get; set; }
        public string Type { get; set; }
        public FluffyVip Vip { get; set; }
    }

    public class FluffyAvatar {
        public ContainerSize ContainerSize { get; set; }
        public FluffyFallbackLayers FallbackLayers { get; set; }
        public string Mid { get; set; }
    }

    public class FluffyFallbackLayers {
        public bool IsCriticalGroup { get; set; }
        public List<FluffyLayer> Layers { get; set; }
    }

    public class FluffyLayer {
        public GeneralSpec GeneralSpec { get; set; }
        public FluffyLayerConfig LayerConfig { get; set; }
        public FluffyResource Resource { get; set; }
        public bool Visible { get; set; }
    }

    public class FluffyLayerConfig {
        public bool? IsCritical { get; set; }
        public FluffyTags Tags { get; set; }
    }

    public class FluffyTags {
        public Layer AvatarLayer { get; set; }
        public GeneralCfg GeneralCfg { get; set; }
        public Layer IconLayer { get; set; }
        public Layer PendentLayer { get; set; }
    }

    public class FluffyResource {
        public FluffyResImage ResImage { get; set; }
        public long ResType { get; set; }
    }

    public class FluffyResImage {
        public FluffyImageSrc ImageSrc { get; set; }
    }

    public class FluffyImageSrc {
        public long? Placeholder { get; set; }
        public FluffyRemote Remote { get; set; }
        public long SrcType { get; set; }
        public long? Local { get; set; }
    }

    public class FluffyRemote {
        public string BfsStyle { get; set; }
        public Uri Url { get; set; }
    }

    public class FluffyPendant {
        public long Expire { get; set; }
        public string Image { get; set; }
        public string ImageEnhance { get; set; }
        public string ImageEnhanceFrame { get; set; }
        public long NPid { get; set; }
        public string Name { get; set; }
        public long Pid { get; set; }
    }

    public class FluffyVip {
        public long AvatarSubscript { get; set; }
        public string AvatarSubscriptUrl { get; set; }
        public long DueDate { get; set; }
        public FluffyLabel Label { get; set; }
        public string NicknameColor { get; set; }
        public long Status { get; set; }
        public long ThemeType { get; set; }
        public long Type { get; set; }
    }

    public class FluffyLabel {
        public string BgColor { get; set; }
        public long BgStyle { get; set; }
        public string BorderColor { get; set; }
        public Uri ImgLabelUriHans { get; set; }
        public Uri ImgLabelUriHansStatic { get; set; }
        public string ImgLabelUriHant { get; set; }
        public Uri ImgLabelUriHantStatic { get; set; }
        public string LabelTheme { get; set; }
        public Uri Path { get; set; }
        public string Text { get; set; }
        public string TextColor { get; set; }
        public bool UseImgLabel { get; set; }
    }

    public class FluffyModuleDynamic {
        public object Additional { get; set; }
        public SummaryClass Desc { get; set; }
        public FluffyMajor Major { get; set; }
        public object Topic { get; set; }
    }

    public class FluffyMajor {
        public Archive Archive { get; set; }
        public string Type { get; set; }
    }

    #endregion

    protected override string Url
        => "https://api.bilibili.com/x/polymer/web-dynamic/v1/feed/space?host_mid={id}&offset={offset}";

    protected override HttpMethod Method => HttpMethod.Get;

    public UploaderVideoRpc(string uploaderId, string offset = "") {
        Parameters = new() {
            { "id", uploaderId },
            { "offset", offset }
        };
    }
}
