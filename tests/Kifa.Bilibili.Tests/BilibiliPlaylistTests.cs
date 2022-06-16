using Kifa.Bilibili.BilibiliApi;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliPlaylistTests {
    [Fact]
    public void RpcTest() {
        Assert.True(new PlaylistRpc().Invoke("743911266").Data.Medias.Count > 50);
    }

    [Fact]
    public void FillTest() {
        var playlist = new BilibiliPlaylist {
            Id = "743911266"
        };
        playlist.Fill();
        Assert.Equal("🎸", playlist.Title);
        Assert.Equal("斗勇猫晶晶", playlist.Uploader);
        Assert.True(playlist.Videos.Count > 50);
    }
}
