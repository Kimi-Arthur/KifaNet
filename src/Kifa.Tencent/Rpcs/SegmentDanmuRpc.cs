using Kifa.Rpc;

namespace Kifa.Tencent.Rpcs;

sealed class SegmentDanmuRpc : KifaJsonParameterizedRpc<SegmentDanmuRpc.Response> {
    #region SegmentDanmuRpc.Response

    public class Response {
        public List<TencentDanmu> BarrageList { get; set; }
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
