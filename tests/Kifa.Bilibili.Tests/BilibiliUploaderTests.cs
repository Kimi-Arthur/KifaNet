using System.Linq;
using FluentAssertions;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Configs;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliUploaderTests {
    public BilibiliUploaderTests() {
        KifaConfigs.Init();
    }

    [Fact]
    public void UploaderVideoRpcTest() {
        HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc("43536")).Data.Cards.Should()
            .HaveCount(20);
    }

    [Fact]
    public void UploaderInfoRpcTest() {
        Assert.Equal("黑桐谷歌",
            HttpClients.GetBilibiliClient().Call(new UploaderInfoWebRpc("43536")).Space.Info.Name);
    }

    [Fact]
    public void FillTest() {
        var uploader = new BilibiliUploader {
            Id = "1462401621"
        };
        uploader.Fill();
        Assert.Equal("特厨隋卞", uploader.Name);
        uploader.Aids.Should().HaveCountGreaterThan(300);
        uploader.Aids.Should().Contain("av724849048");
    }
}
