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
    public void VideoRpcTest() {
        HttpClients.BilibiliHttpClient.Call(new UploaderVideoRpc("43536")).Data.Cards.Should()
            .HaveCount(20);
    }

    [Fact]
    public void InfoRpcTest() {
        Assert.Equal("黑桐谷歌",
            HttpClients.BilibiliHttpClient.Call(new UploaderInfoRpc("43536")).Data.Name);
    }

    [Fact]
    public void FillTest() {
        var uploader = new BilibiliUploader {
            Id = "43536"
        };
        uploader.Fill();
        Assert.Equal("黑桐谷歌", uploader.Name);
        uploader.Aids.Should().HaveCountGreaterThan(200);
        Assert.Equal("av27700", uploader.Aids.First());
    }
}
