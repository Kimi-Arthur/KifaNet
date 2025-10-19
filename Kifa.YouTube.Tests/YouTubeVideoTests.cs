using FluentAssertions;
using Kifa.Configs;

namespace Kifa.YouTube.Tests;

public class YouTubeVideoTests {
    public YouTubeVideoTests() {
        KifaConfigs.Init();
    }

    [Fact]
    public void FillWithYoutubeDlTest() {
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
    public void FillWithWaybackTest() {
        var video = new YouTubeVideo {
            Id = "0iNrY1ixR8I"
        };

        const string expectedVideo = """
                                     {
                                       "id": "0iNrY1ixR8I",
                                       "title": "Taylor Swift - Red Carpet Interview (2013 AMAs)",
                                       "author": "TaylorSwiftVEVO",
                                       "description": "Lance Bass and Jordin Sparks interview Taylor Swift on the red carpet at the American Music Awards 2013. She discusses her AMA nominations, being named the biggest Pop Star in the world by New York Magazine and her new movie.",
                                       "duration": "00:03:21",
                                       "width": 1280,
                                       "height": 720
                                     }
                                     """;

        video.Fill();
        video.ToString().Should().Be(expectedVideo);
    }

    [Fact]
    public void FillWithFindYoutubeVideoTest() {
        var video = new YouTubeVideo {
            Id = "-mvEt8ZLsX4"
        };

        const string expectedVideo = """
                                     {
                                       "id": "-mvEt8ZLsX4",
                                       "title": "AKB48 恋するフォーチュンクッキー KOREA COVER DANCE ' HHO48 ' IN 사통팔달",
                                       "author": "베레스트(Verest) 360 VR",
                                       "upload_date": "2015-01-03",
                                       "description": "사통팔달 행사영상\n커버댄스 걸그룹 HHO48\nAKB48-恋するフォーチュンクッキー\n재미있게 봐주세요.^^\n다른 팀들의 영상도 차후 업로드 됩니다.",
                                       "categories": [
                                         "Music"
                                       ],
                                       "tags": [
                                         "AKB48 (Award Winner)",
                                         "Dance Music (Musical Genre)",
                                         "Dance-pop (Musical Genre)",
                                         "Dance (Interest)",
                                         "Dancehall (Musical Genre)",
                                         "Dance Dance Revolution (Video Game Series)",
                                         "恋するフォーチュンクッキー (Musical Recording)",
                                         "K-pop Cover Dance Festival",
                                         "J-pop (Musical Genre)",
                                         "Music (TV Genre)",
                                         "South Korea (Country)",
                                         "Country (Musical Genre)",
                                         "Television (Invention)",
                                         "North",
                                         "Carolina"
                                       ],
                                       "duration": "00:03:08",
                                       "fps": 24.0,
                                       "width": 1920,
                                       "height": 1080,
                                       "format_id": "248+171",
                                       "thumbnail": "https://i.ytimg.com/vi/-mvEt8ZLsX4/maxresdefault.jpg"
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
