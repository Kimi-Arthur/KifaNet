using Kifa.Bilibili.BilibiliApi;
using Kifa.Configs;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliTrackingTests {
    public BilibiliTrackingTests() {
        KifaConfigs.Init();
    }

    [Fact]
    public void EnableTrackingTest() {
        Assert.Equal(0, HttpClients.GetBilibiliClient().Call(new TrackingRpc(false)).Code);
    }

    [Fact]
    public void DisableTrackingTest() {
        Assert.Equal(0, HttpClients.GetBilibiliClient().Call(new TrackingRpc(true)).Code);
    }
}
