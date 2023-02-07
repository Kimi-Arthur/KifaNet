using Kifa.Rpc;

namespace Kifa.Tencent.Rpcs;

sealed class SegmentDanmuRpc : KifaJsonParameterizedRpc<SegmentDanmuRpc.Response> {
    #region SegmentDanmuRpc.Response

    public class Response {
        public List<BarrageList> BarrageList { get; set; }
    }

    public class BarrageList {
        public string Id { get; set; }
        public long IsOp { get; set; }
        public string HeadUrl { get; set; }
        public string TimeOffset { get; set; }
        public string UpCount { get; set; }
        public string BubbleHead { get; set; }
        public string BubbleLevel { get; set; }
        public string BubbleId { get; set; }
        public long RickType { get; set; }
        public string ContentStyle { get; set; }
        public long UserVipDegree { get; set; }
        public string CreateTime { get; set; }
        public string Content { get; set; }
        public long HotType { get; set; }
        public object GiftInfo { get; set; }
        public object ShareItem { get; set; }
        public string Vuid { get; set; }
        public string Nick { get; set; }
        public string DataKey { get; set; }
        public double ContentScore { get; set; }
        public long ShowWeight { get; set; }
        public long TrackType { get; set; }
        public long ShowLikeType { get; set; }
        public long ReportLikeScore { get; set; }
    }

    #endregion

    public override string UrlPattern
        => "https://dm.video.qq.com/barrage/segment/{video_id}/{segment_id}";

    public override HttpMethod Method => HttpMethod.Get;

    internal SegmentDanmuRpc(string videoId, string segmentId) {
        parameters = new Dictionary<string, string> {
            { "video_id", videoId },
            { "segment_id", segmentId }
        };
    }
}
