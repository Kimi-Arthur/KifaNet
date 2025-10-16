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
        HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc("43536")).Data.Items.Should()
            .HaveCountGreaterThan(10);
    }

    [Fact]
    public void UploaderInfoRpcTest() {
        Assert.Equal("黑桐谷歌",
            HttpClients.GetBilibiliClient().Call(new UploaderInfoWebRpc("43536")).Space.Info.Name);
    }

    [Fact]
    public void FillTest() {
        var uploader = new BilibiliUploader {
            Id = "18427691"
        };
        uploader.Fill();
        uploader.Name.Should().Be("壹壹yeamusic");
        uploader.Aids[^1].Should().Be("av561513930");
        uploader.Aids.Should().HaveCount(104);
    }
}
