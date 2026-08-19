using FluentAssertions;
using Kifa.Configs;

namespace Kifa.YouTube.Tests;

public class YouTubeUploaderTests {
    public YouTubeUploaderTests() {
        KifaConfigs.Init();
    }

    [Fact]
    public void UploaderModelTest() {
        var uploader = new YouTubeUploader {
            Id = "@TestChannel",
            Name = "Test Channel",
            Videos = ["vid1", "vid2"]
        };

        uploader.Id.Should().Be("@TestChannel");
        uploader.Name.Should().Be("Test Channel");
        uploader.Videos.Should().Equal("vid1", "vid2");
    }

    [Fact]
    public void FillTest() {
        var uploader = new YouTubeUploader {
            Id = "@Google"
        };

        uploader.Fill();
        uploader.Name.Should().Be("Google");
        uploader.Videos.Count.Should().BeGreaterThanOrEqualTo(2000);
        uploader.Videos[0].Should().Be("bSp-foRDH5M");
    }
}
