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
            NameAliases = ["Old Channel", "Alternative Name"],
            ChannelId = "UC1234567890abcdefghij",
            Videos = ["vid1", "vid2"]
        };

        uploader.Id.Should().Be("@TestChannel");
        uploader.Name.Should().Be("Test Channel");
        uploader.NameAliases.Should().Equal("Old Channel", "Alternative Name");
        uploader.ChannelId.Should().Be("UC1234567890abcdefghij");
        uploader.Videos.Should().Equal("vid1", "vid2");
        uploader.GetUploaderFolder().NormalizeFilePath()
            .Should().Be("Test Channel.@TestChannel.youtube");
        uploader.GetVirtualItems().Should().BeEquivalentTo([
            "/$/UC1234567890abcdefghij",
            "/$/TestChannel",
            "/$/Test Channel",
            "/$/Old Channel",
            "/$/Alternative Name"
        ]);
    }

    [Fact]
    public void FillTest() {
        var uploader = new YouTubeUploader {
            Id = "@Google"
        };

        uploader.Fill();
        uploader.Name.Should().Be("Google");
        uploader.ChannelId.Should().Be("UCK8sQmJBp8GCxrOtXWBpyEA");
        uploader.GetUploaderFolder().NormalizeFilePath().Should().Be("Google.@Google.youtube");
        uploader.Videos.Count.Should().BeGreaterThanOrEqualTo(2000);
        uploader.Videos.Should().Contain("bSp-foRDH5M");
    }

    [Fact]
    public void FillShortsTest() {
        var uploader = new YouTubeUploader {
            Id = "@fcbayern"
        };

        uploader.Fill();
        uploader.Name.Should().Be("FC Bayern Munich");
        uploader.ChannelId.Should().Be("UCZkcxFIsqW5htimoUQKA0iA");
        uploader.GetUploaderFolder().NormalizeFilePath()
            .Should().Be("FC Bayern Munich.@fcbayern.youtube");
        uploader.Videos.Should().Contain("WUj_TgtrTJE");
    }
}
