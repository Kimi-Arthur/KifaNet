using FluentAssertions;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Configs;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliPlaylistTests {
    public BilibiliPlaylistTests() {
        KifaConfigs.Init();
    }

    [Fact]
    public void RpcTest() {
        BilibiliVideo.GetBilibiliClient().Call(new PlaylistRpc("743911266")).Data.Medias.Should()
            .HaveCount(20);
    }

    [Fact]
    public void FillTest() {
        var playlist = new BilibiliPlaylist {
            Id = "743911266"
        };
        playlist.Fill();
        Assert.Equal("🎸", playlist.Title);
        Assert.Equal("斗勇猫晶晶", playlist.Uploader);
        playlist.Videos.Should().HaveCountGreaterThan(50);
    }
}
