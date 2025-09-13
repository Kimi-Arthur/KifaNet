using FluentAssertions;
using Xunit;

namespace Kifa.YouTube.Tests;

public class YouTubeVideoTests {
    [Fact]
    public void FillTest() {
        var video = new YouTubeVideo {
            Id = "_ox9gJZ8ENo"
        };

        const string expectedVideo = """
                                     {
                                       "id": "_ox9gJZ8ENo",
                                       "title": "SNH48 《呜吒》 (UZA) Rehearsal Footage",
                                       "author": "SNH48s",
                                       "upload_date": "2014-08-22",
                                       "description": "2014-08-22\nThis footage was revealed on 8/22 at the send-off ceremony (also Wu Zhehan's birthday show) held at the SNH48 Star Dream Theater. The top 16 voted girls from the recent General Election will be flying to South Korea to film the 'UZA' music video!\n\nhttp://shanghai48s.com/tagged/UZA\nhttp://shanghai48s.com/\n\nSNH48赴韩拍摄《呜吒》MV 出征VCR\nhttp://www.tudou.com/programs/view/oFrkZWlUec4/",
                                       "categories": [
                                         "Entertainment"
                                       ],
                                       "tags": [
                                         "SNH48"
                                       ],
                                       "duration": "00:03:54",
                                       "fps": 25.0,
                                       "width": 1280,
                                       "height": 720,
                                       "format_id": "136+140",
                                       "thumbnail": "https://i.ytimg.com/vi/_ox9gJZ8ENo/maxresdefault.jpg"
                                     }
                                     """;

        video.Fill();
        video.ToString().Should().Be(expectedVideo);
    }

    [Fact]
    public void NameTest() {
        var video = new YouTubeVideo {
            Id = "RWrSo_7RmgQ",
            Title = "我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409",
            Author = "中国湖南卫视官方频道 China HunanTV Official Channel",
            FormatId = "137+22"
        };

        video.GetCanonicalNames().Should().BeEquivalentTo(["RWrSo_7RmgQ.137+22", "RWrSo_7RmgQ"]);
        video.GetDesiredName().Should()
            .Be(
                "中国湖南卫视官方频道 China HunanTV Official Channel/我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409.RWrSo_7RmgQ.137+22");
    }
}
