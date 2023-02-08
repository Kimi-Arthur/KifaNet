using System.Linq;
using System.Net.Http;
using Kifa.Tencent.Rpcs;
using Xunit;

namespace Kifa.Tencent.Tests;

public class DanmuTests {
    HttpClient httpClient = new HttpClient();

    [Fact]
    public void BaseDanmuRpcTest() {
        var response = httpClient.Call(new BaseDanmuRpc("i0045u918s5"));
        Assert.Equal(88, response.SegmentIndex.Count);
        Assert.Equal("t/v1/2100000/2130000", response.SegmentIndex["2100000"].SegmentName);
    }

    [Fact]
    public void SegmentDanmuRpcTest() {
        var response = httpClient.Call(new SegmentDanmuRpc("i0045u918s5", "t/v1/2100000/2130000"));
        Assert.Equal(600, response.BarrageList.Count);
        Assert.Equal("把心脏起搏器按停了",
            response.BarrageList.First(b => b.Id == "76561198061395137").Content);
        Assert.Equal(
            "{\"color\":\"ffffff\",\"gradient_colors\":[\"CD87FF\",\"CD87FF\"],\"position\":1}",
            response.BarrageList.First(b => b.Id == "76561198061432944").ContentStyle);
    }
}
