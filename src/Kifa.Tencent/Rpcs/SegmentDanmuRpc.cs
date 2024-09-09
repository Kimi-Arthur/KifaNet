using Kifa.Rpc;

namespace Kifa.Tencent.Rpcs;

sealed class SegmentDanmuRpc : KifaJsonParameterizedRpc<SegmentDanmuRpc.Response> {
    #region SegmentDanmuRpc.Response

    public class Response {
        public List<TencentDanmu> BarrageList { get; set; }
    }

    #endregion

    protected override string Url
        => "https://dm.video.qq.com/barrage/segment/{video_id}/{segment_id}";

    protected override HttpMethod Method => HttpMethod.Get;

    internal SegmentDanmuRpc(string videoId, string segmentId) {
        Parameters = new () {
            { "video_id", videoId },
            { "segment_id", segmentId }
        };
    }
}
