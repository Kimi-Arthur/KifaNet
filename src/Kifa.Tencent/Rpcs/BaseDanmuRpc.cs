using System.Runtime.CompilerServices;
using Kifa.Rpc;

[assembly: InternalsVisibleTo("Kifa.Tencent.Tests")]

namespace Kifa.Tencent.Rpcs;

sealed class BaseDanmuRpc : KifaJsonParameterizedRpc<BaseDanmuRpc.Response> {
    #region BaseDanmuRpc.Response

    public class Response {
        public string Total { get; set; }
        public string CountToDisplay { get; set; }
        public List<object> FirstSegment { get; set; }
        public Dictionary<string, SegmentIndex> SegmentIndex { get; set; }
        public string SegmentStart { get; set; }
        public string SegmentSpan { get; set; }
        public LeadBarrage LeadBarrage { get; set; }
        public string CheckUpTime { get; set; }
    }

    public class LeadBarrage {
        public string Content { get; set; }
        public long Type { get; set; }
        public string LeftIconUrl { get; set; }
        public string RightIconUrl { get; set; }
        public long ActivityType { get; set; }
        public string ActivityUrl { get; set; }
        public string BackColor { get; set; }
        public string UpdateTime { get; set; }
        public List<object> BackGradientColor { get; set; }
        public string ContentStyle { get; set; }
    }

    public class SegmentIndex {
        public string SegmentStart { get; set; }
        public string SegmentName { get; set; }
    }

    #endregion

    public override string UrlPattern => "https://dm.video.qq.com/barrage/base/{video_id}";
    public override HttpMethod Method => HttpMethod.Get;

    public BaseDanmuRpc(string videoId) {
        parameters = new Dictionary<string, string> {
            { "video_id", videoId }
        };
    }
}
