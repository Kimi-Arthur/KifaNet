using System.Linq;
using FluentAssertions;
using Kifa.Configs;
using YoutubeDLSharp.Options;

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
                                       "author_id": "@SNH48s",
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
                                       "codec": "avc",
                                       "format_id": "136+140",
                                       "thumbnail": "https://i.ytimg.com/vi/_ox9gJZ8ENo/maxresdefault.jpg"
                                     }
                                     """;

        video.Fill();
        video.ToString().Should().Be(expectedVideo);
    }

    [Fact]
    public void PluginDirsOptionTest() {
        YouTubeVideo.PluginPath = "/tmp";
        var options = YouTubeVideo.GetOptionSet();
        options.GetOptionFlags().Should().ContainMatch("*--plugin-dirs*");
        options.GetOptionFlags().Should().ContainMatch("*/tmp*");
    }

    [Fact]
    public void ExtractorArgsOptionTest() {
        YouTubeVideo.ExtractorArgs =
            ["youtube:player_client=ios,android", "youtubetab:approximate_date"];
        var options = YouTubeVideo.GetOptionSet();
        var flags = options.GetOptionFlags().ToList();
        flags.Should().ContainMatch("*--extractor-args*");
        flags.Should().ContainMatch("*youtube:player_client=ios,android*");
        flags.Should().ContainMatch("*youtubetab:approximate_date*");
    }

    [Fact]
    public void TrackDownloadOptionsTest() {
        var options = new OptionSet {
            Format = "395,251",
            WriteThumbnail = true,
            ConvertThumbnails = "png",
            Output = "/tmp/test.%(format_id)s.%(ext)s"
        };
        options.AddCustomOption("-o", "thumbnail:/tmp/test.c.%(ext)s");
        var flags = options.GetOptionFlags().ToList();
        flags.Should().ContainMatch("*-o \"/tmp/test.%(format_id)s.%(ext)s\"*");
        flags.Should().ContainMatch("*-o \"thumbnail:/tmp/test.c.%(ext)s\"*");
        flags.Should().ContainMatch("*-f \"395,251\"*");
        flags.Should().Contain("--write-thumbnail");
        flags.Should().ContainMatch("*--convert-thumbnails \"png\"*");
    }

    [Fact]
    public void FillWithWaybackTest() {
        var video = new YouTubeVideo {
            Id = "0iNrY1ixR8I"
        };

        const string expectedVideo = """
                                     {
                                       "id": "0iNrY1ixR8I",
                                       "title": "A Day in the Life of a Software Engineer in London",
                                       "author": "Mayuko",
                                       "upload_date": "2019-10-09",
                                       "description": "I spent a week in London and decided to film a Day in the Life video as a Software Engineer there! Thank you so much to Indeed Prime for sponsoring this video! If you're a Software Engineer looking for a job in London or the UK, check them out here: https://indeed.com/mayuko\n\nHope you guys enjoy this Day in the Life of a Software Engineer in London! Definitely miss being there, hope you guys have an awesome rest of the week!\n\n---\n\nDon't forget to subscribe!  http://bit.ly/2qfc1tP\n\n\nSpecial thanks to:\nKasia - http://instagram.com/kasiarun\nDan - https://www.instagram.com/d_a_n_w_o_o_d/\n\nEquipment used:\nCanon G7X Mark II - https://amzn.to/2J2eM8x\nSony A6500 - http://amzn.to/2p4bBVx\nSony 16-50mm f/3.5-5.6 OSS Lens - http://amzn.to/2p5jXo7\nSony 35mm f/1.8 - http://amzn.to/2oB5Kk1\nSony 10-18mm f/4 - http://amzn.to/2nDEmiz\n\n\nMusic by:\nEpidemic Sound - https://share.epidemicsound.com/mhellonearth\n\n\nAbout Mayuko:\nHello! My name is Mayuko, and I'm a Content Creator. Previously, I was an iOS Software Engineer working in Silicon Valley for over 6 years, working at companies like Intuit, Patreon, & Chewse. On this channel, I produce videos around technology, career, and lifestyle through the lens of a software engineer. \n\nCheck out my other videos!\nDay in the Life of a Software Engineer in Silicon Valley: \nhttps://youtu.be/4voHu_sM2AU\nHow I Became a Software Engineer: \nhttps://youtu.be/cKzpnffE3Vo\nA Day in the Life of a Senior Software Engineer: \nhttps://youtu.be/44ptms_0D4Y\n\n\nAll things Mayuko:\nSite - http://hellomayuko.com\nInstagram - https://www.instagram.com/hellomayuko/\nTwitter - https://twitter.com/hellomayuko\n\n\nFTC: This video is sponsored by Indeed Prime.",
                                       "categories": [
                                         "Science & Technology"
                                       ],
                                       "duration": "00:09:44",
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
                                       "author_id": "Verest2014",
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
                                       "codec": "vp9",
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
            AuthorId = "HunanTV",
            Width = 1920,
            Height = 1080,
            Fps = 60,
            FormatId = "137+22"
        };

        video.GetCanonicalNames().Should().BeEquivalentTo(["RWrSo_7RmgQ.1920x1080p60.137+22"]);
        video.GetDesiredName().Should()
            .Be(
                "中国湖南卫视官方频道 China HunanTV Official Channel.HunanTV.youtube/我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409.RWrSo_7RmgQ.1920x1080p60.137+22");
        video.GetDesiredName(alternativeFolder: $"{video.Title}.{video.Id}").Should().Be(
            "我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409.RWrSo_7RmgQ.youtube/我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409.RWrSo_7RmgQ.1920x1080p60.137+22");
        video.GetDesiredName(prefix: "2014-04-09").Should().Be(
            "中国湖南卫视官方频道 China HunanTV Official Channel.HunanTV.youtube/2014-04-09 我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409.RWrSo_7RmgQ.1920x1080p60.137+22");

        video.GetCanonicalNames(includeFormat: false).Should().BeEquivalentTo(["RWrSo_7RmgQ"]);
        video.GetDesiredName(includeFormat: false).Should().Be(
            "中国湖南卫视官方频道 China HunanTV Official Channel.HunanTV.youtube/我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409.RWrSo_7RmgQ");

        var videoWithResolutionOnly = new YouTubeVideo {
            Id = "RWrSo_7RmgQ",
            Title = "我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409",
            Author = "中国湖南卫视官方频道 China HunanTV Official Channel",
            AuthorId = "HunanTV",
            Width = 1280,
            Height = 720
        };

        videoWithResolutionOnly.GetCanonicalNames().Should().BeEquivalentTo(["RWrSo_7RmgQ.1280x720p"]);
        videoWithResolutionOnly.GetDesiredName().Should().Be(
            "中国湖南卫视官方频道 China HunanTV Official Channel.HunanTV.youtube/我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409.RWrSo_7RmgQ.1280x720p");

        var videoWithFormatIdOnly = new YouTubeVideo {
            Id = "RWrSo_7RmgQ",
            Title = "我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409",
            Author = "中国湖南卫视官方频道 China HunanTV Official Channel",
            AuthorId = "HunanTV",
            FormatId = "137+22"
        };

        videoWithFormatIdOnly.GetCanonicalNames().Should().BeEquivalentTo(["RWrSo_7RmgQ.137+22"]);
        videoWithFormatIdOnly.GetDesiredName().Should().Be(
            "中国湖南卫视官方频道 China HunanTV Official Channel.HunanTV.youtube/我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409.RWrSo_7RmgQ.137+22");

        var videoWithAvc = new YouTubeVideo {
            Id = "RWrSo_7RmgQ",
            Title = "我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409",
            Author = "中国湖南卫视官方频道 China HunanTV Official Channel",
            AuthorId = "HunanTV",
            Width = 1920,
            Height = 1080,
            Fps = 60,
            Codec = "avc1.640028",
            FormatId = "137+22"
        };

        videoWithAvc.GetCanonicalNames().Should().BeEquivalentTo(["RWrSo_7RmgQ.1920x1080p60.137+22"]);
        videoWithAvc.GetDesiredName().Should().Be(
            "中国湖南卫视官方频道 China HunanTV Official Channel.HunanTV.youtube/我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409.RWrSo_7RmgQ.1920x1080p60.137+22");

        var videoWithVp9 = new YouTubeVideo {
            Id = "RWrSo_7RmgQ",
            Title = "我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409",
            Author = "中国湖南卫视官方频道 China HunanTV Official Channel",
            AuthorId = "HunanTV",
            Width = 1920,
            Height = 1080,
            Fps = 60,
            Codec = "vp09.00.41.08",
            FormatId = "248+171"
        };

        videoWithVp9.GetCanonicalNames().Should().BeEquivalentTo(["RWrSo_7RmgQ.1920x1080p60-vp9.248+171"]);
        videoWithVp9.GetDesiredName().Should().Be(
            "中国湖南卫视官方频道 China HunanTV Official Channel.HunanTV.youtube/我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409.RWrSo_7RmgQ.1920x1080p60-vp9.248+171");

        var videoWithAv1 = new YouTubeVideo {
            Id = "RWrSo_7RmgQ",
            Title = "我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409",
            Author = "中国湖南卫视官方频道 China HunanTV Official Channel",
            AuthorId = "HunanTV",
            Width = 1920,
            Height = 1080,
            Fps = 60,
            Codec = "av01.0.08M.08",
            FormatId = "399+140"
        };

        videoWithAv1.GetCanonicalNames().Should().BeEquivalentTo(["RWrSo_7RmgQ.1920x1080p60-av1.399+140"]);
        videoWithAv1.GetDesiredName().Should().Be(
            "中国湖南卫视官方频道 China HunanTV Official Channel.HunanTV.youtube/我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409.RWrSo_7RmgQ.1920x1080p60-av1.399+140");

        var videoWithoutFormat = new YouTubeVideo {
            Id = "RWrSo_7RmgQ",
            Title = "我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409",
            Author = "中国湖南卫视官方频道 China HunanTV Official Channel",
            AuthorId = "HunanTV"
        };

        videoWithoutFormat.GetCanonicalNames().Should().BeEquivalentTo(["RWrSo_7RmgQ"]);
        videoWithoutFormat.GetDesiredName().Should().Be(
            "中国湖南卫视官方频道 China HunanTV Official Channel.HunanTV.youtube/我是歌手-第二季-品冠演唱串烧-【湖南卫视官方版1080P】20140409.RWrSo_7RmgQ");

        // Long title chopped with suffix preserved
        var longVideo = new YouTubeVideo {
            Id = "RWrSo_7RmgQ",
            Title = new string('a', 300),
            Author = "Author",
            AuthorId = "123",
            Width = 1920,
            Height = 1080,
            Fps = 60,
            FormatId = "137+22"
        };

        var longDesiredName = longVideo.GetDesiredName();
        var expectedSuffix = ".RWrSo_7RmgQ.1920x1080p60.137+22";
        longDesiredName.Should().StartWith("Author.123.youtube/");
        longDesiredName.Should().EndWith(expectedSuffix);
        var filePart = longDesiredName!["Author.123.youtube/".Length..];
        System.Text.Encoding.UTF8.GetByteCount(filePart).Should().BeLessThanOrEqualTo(246);

        // Long author chopped with .123.youtube suffix preserved
        var longAuthorVideo = new YouTubeVideo {
            Id = "RWrSo_7RmgQ",
            Title = "Short Title",
            Author = new string('x', 300),
            AuthorId = "123"
        };

        var longAuthorDesiredName = longAuthorVideo.GetDesiredName();
        longAuthorDesiredName.Should().EndWith(".123.youtube/Short Title.RWrSo_7RmgQ");
        var authorFolder = longAuthorDesiredName!.Split('/')[0];
        System.Text.Encoding.UTF8.GetByteCount(authorFolder).Should().BeLessThanOrEqualTo(255);
        authorFolder.Should().EndWith("~.123.youtube");

        // Special character normalization
        var slashVideo = new YouTubeVideo {
            Id = "RWrSo_7RmgQ",
            Title = "Fate/Zero: Episode 01? *Prologue*",
            Author = "Studio/Trigger",
            AuthorId = "123"
        };

        var slashDesiredName = slashVideo.GetDesiredName();
        slashDesiredName.Should().Be("Studio／Trigger.123.youtube/Fate／Zero：Episode 01？ ＊Prologue＊.RWrSo_7RmgQ");
    }
}
