using FluentAssertions;
using Xunit;

namespace Kifa.YouTube.Tests;

public class YouTubeVideoTests {
    [Fact]
    public void NameTest() {
        var video = new YouTubeVideo {
            Id = "RWrSo_7RmgQ",
            Title = "我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409",
            Author = "中国湖南卫视官方频道 China HunanTV Official Channel",
            FormatId = "137+22"
        };

        video.GetCanonicalName().Should().Be("RWrSo_7RmgQ.137+22");
        video.GetDesiredName().Should()
            .Be(
                "中国湖南卫视官方频道 China HunanTV Official Channel/我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409.RWrSo_7RmgQ.137+22");
    }
}
