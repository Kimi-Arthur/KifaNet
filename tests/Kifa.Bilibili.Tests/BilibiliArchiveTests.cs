using FluentAssertions;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Configs;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliArchiveTests {
    public BilibiliArchiveTests() {
        KifaConfigs.Init();
    }

    [Fact]
    public void RpcTest() {
        HttpClients.BilibiliHttpClient
            .Call(new ArchiveRpc(uploaderId: "43536", seasonId: "1808473")).Data.Checked().Aids
            .Should().HaveCountGreaterOrEqualTo(6);
    }

    [Fact]
    public void FillTest() {
        var archive = new BilibiliArchive {
            Id = "43536/820817"
        };
        archive.Fill();

        Assert.Equal("合集·【艾尔登法环】全流程地毯式超详细黑桐谷歌游戏视频解说", archive.Title);
        Assert.Equal("黑桐谷歌", archive.Author);
        archive.Videos.Should().HaveCountGreaterOrEqualTo(52);
    }
}
