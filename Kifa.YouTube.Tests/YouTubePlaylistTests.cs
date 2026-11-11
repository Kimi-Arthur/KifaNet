using System.Linq;
using FluentAssertions;
using Kifa.Configs;

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
            Videos = ["vid1", "vid2"],
            RemovedVideos = ["vid0"]
        };

        playlist.Id.Should().Be("PL12345");
        playlist.Title.Should().Be("Test Playlist");
        playlist.Author.Should().Be("Test Author");
        playlist.Videos.Should().Equal("vid1", "vid2");
        playlist.RemovedVideos.Should().Equal("vid0");
    }

    [Fact]
    public void FillTest() {
        var playlist = new YouTubePlaylist {
            Id = "PLRqwX-V7Uu6ZiZxtDDRCi6uhfTH4FilpH"
        };

        playlist.Fill();
        playlist.Title.Should().Be("Coding Challenges");
        playlist.Author.Should().Be("The Coding Train");
        playlist.Videos.Count.Should().BeGreaterThanOrEqualTo(100);
        playlist.Videos.Should().Contain("17WoOqgXsRM");
    }

    [Fact]
    public void FillUnlistedPlaylistTest() {
        var playlist = new YouTubePlaylist {
            Id = "PLgK1iAEEioHYcAfLrVyq9UX6n7uz3sHeG"
        };

        playlist.Fill();
        playlist.Videos.Should().NotBeEmpty();
    }

    [Fact]
    public void UpdateVideosTracksRemovedVideosTest() {
        var playlist = new YouTubePlaylist {
            Id = "PL12345",
            Videos = ["vid1", "removedVid1", "vid2"],
            RemovedVideos = ["oldRemovedVid", "reAddedVid"]
        };

        playlist.UpdateVideos(["vid1", "vid2", "reAddedVid", "newTailVid"]);

        playlist.Videos.Should().Equal("vid1", "vid2", "reAddedVid", "newTailVid");
        playlist.RemovedVideos.Should().Equal("oldRemovedVid", "removedVid1");
    }

    [Fact]
    public void FillRefreshAndRemovedVideosTest() {
        var fullPlaylist = new YouTubePlaylist {
            Id = "PLRqwX-V7Uu6ZiZxtDDRCi6uhfTH4FilpH"
        };
        fullPlaylist.Fill();

        var refreshedPlaylist = new YouTubePlaylist {
            Id = "PLRqwX-V7Uu6ZiZxtDDRCi6uhfTH4FilpH",
            Videos = [..fullPlaylist.Videos.Take(10), "removedVid1"],
            RemovedVideos = ["oldRemovedVid", fullPlaylist.Videos[0]]
        };

        refreshedPlaylist.Fill();
        refreshedPlaylist.Title.Should().Be("Coding Challenges");
        refreshedPlaylist.Author.Should().Be("The Coding Train");
        refreshedPlaylist.Videos.Should().Equal(fullPlaylist.Videos);
        refreshedPlaylist.RemovedVideos.Should().Equal("oldRemovedVid", "removedVid1");
    }
}
