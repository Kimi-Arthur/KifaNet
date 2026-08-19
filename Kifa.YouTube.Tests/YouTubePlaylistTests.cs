using FluentAssertions;
using Kifa.Configs;
using Xunit;

namespace Kifa.YouTube.Tests;

public class YouTubePlaylistTests {
    public YouTubePlaylistTests() {
        KifaConfigs.Init();
    }

    [Fact]
    public void PlaylistModelTest() {
        var playlist = new YouTubePlaylist {
            Id = "PL12345",
            Title = "Test Playlist",
            Author = "Test Author",
            Videos = ["vid1", "vid2"]
        };

        playlist.Id.Should().Be("PL12345");
        playlist.Title.Should().Be("Test Playlist");
        playlist.Author.Should().Be("Test Author");
        playlist.Videos.Should().Equal("vid1", "vid2");
    }

    [Fact]
    public void FillTest() {
        var playlist = new YouTubePlaylist {
            Id = "PLRqwX-V7Uu6ZiZxtDDRCi6uhfTH4FilpH"
        };

        playlist.Fill();
        playlist.Title.Should().Be("Coding Challenges");
        playlist.Author.Should().Be("The Coding Train");
        playlist.Videos.Should().HaveCount(100);
        playlist.Videos[0].Should().Be("17WoOqgXsRM");
        playlist.Videos[^1].Should().Be("N8Fabn1om2k");
    }
}
