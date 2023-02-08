using Kifa.Tencent.Rpcs;

namespace Kifa.Tencent;

public class TencentVideo {
    static readonly HttpClient HttpClient = new();

    // https://v.qq.com/x/cover/mzc002007knmh3g/d0045caapwc.html

    public static IEnumerable<TencentDanmu> GetDanmuList(string videoId) {
        var info = HttpClient.Call(new BaseDanmuRpc(videoId));

        return info.SegmentIndex.Values.OrderBy(segment => int.Parse(segment.SegmentStart))
            .Select(segment => segment.SegmentName).SelectMany(segment => HttpClient
                .Call(new SegmentDanmuRpc(videoId, segment)).BarrageList
                .Select(TencentDanmu.Parse));
    }
}
