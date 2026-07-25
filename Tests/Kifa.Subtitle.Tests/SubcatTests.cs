#nullable enable

using Kifa.Html;
using Kifa.Subtitle.Subcat;
using Xunit;

namespace Kifa.Subtitle.Tests;

public class SubcatTests {
    const string SampleHtml = """
                              <!DOCTYPE html>
                              <html>
                              <body>
                                  <div class="sub-single">
                                      <span>English</span>
                                      <span><a id="download_en" href="/subs/1436/TEST TEST TEST-en.srt" class="green-link">Download</a></span>
                                  </div>
                                  <div class="sub-single">
                                      <span>Chinese (Simplified)</span>
                                      <span><button id="zh-CN" onclick="translate_from_server_folder('zh-CN', 'TEST TEST TEST-orig.srt', '/subs/1436/')" class="yellow-link">Translate</button></span>
                                  </div>
                              </body>
                              </html>
                              """;

    [Fact]
    public void GetDownloadLinkExistingLanguageTest() {
        var doc = SampleHtml.GetDocument();
        var needsGeneration = SubcatClient.GetDownloadLink(doc, "en");
        Assert.NotNull(needsGeneration);
        Assert.False(needsGeneration.Value);

        var choice = new SubcatChoice {
            Id = "1436",
            Title = "TEST TEST TEST",
            Language = "en",
            NeedsGeneration = needsGeneration.Value
        };
        Assert.Equal("https://www.subtitlecat.com/subs/1436/TEST%20TEST%20TEST-en.srt",
            choice.DownloadLink);
        Assert.Equal("https://www.subtitlecat.com/subs/1436/TEST%20TEST%20TEST-orig.srt",
            choice.GenerateLink);
        Assert.Equal("[en] TEST TEST TEST", choice.ToString());
    }

    [Fact]
    public void GetDownloadLinkNonExistingRequestedLanguageTest() {
        var doc = SampleHtml.GetDocument();
        var needsGeneration = SubcatClient.GetDownloadLink(doc, "zh");
        Assert.NotNull(needsGeneration);
        Assert.True(needsGeneration.Value);

        var choice = new SubcatChoice {
            Id = "1436",
            Title = "TEST TEST TEST",
            Language = "zh",
            NeedsGeneration = needsGeneration.Value
        };
        Assert.Equal("https://www.subtitlecat.com/subs/1436/TEST%20TEST%20TEST-zh-CN.srt",
            choice.DownloadLink);
        Assert.Equal("https://www.subtitlecat.com/subs/1436/TEST%20TEST%20TEST-orig.srt",
            choice.GenerateLink);
        Assert.Equal("[zh*] TEST TEST TEST", choice.ToString());
    }

    [Fact]
    public void GetDownloadLinkNotFoundTest() {
        var doc = SampleHtml.GetDocument();
        var needsGeneration = SubcatClient.GetDownloadLink(doc, "fr");
        Assert.Null(needsGeneration);
    }

    [Fact]
    public void GetDownloadLinksTest() {
        var doc = SampleHtml.GetDocument();
        var links = SubcatClient.GetDownloadLinks(doc, ["en", "zh", "fr"]);
        Assert.Equal(2, links.Count);
        Assert.False(links["en"]);
        Assert.True(links["zh"]);
    }

    [Theory]
    [InlineData("https://www.subtitlecat.com/subs/1133/growing_pains_s03e02_aloha_2.html", "1133",
        "growing_pains_s03e02_aloha_2")]
    [InlineData("/subs/1436/TEST_TEST_TEST-orig.srt", "1436", "TEST_TEST_TEST")]
    [InlineData("subs/1133/growing_pains_s03e02_aloha_2.html", "1133",
        "growing_pains_s03e02_aloha_2")]
    [InlineData("https://www.subtitlecat.com/index.php", null, "index")]
    [InlineData("https://www.subtitlecat.com/subs/1509/Life%20on%20Top%201x11%20-%20Growing%20Pains.html", "1509", "Life on Top 1x11 - Growing Pains")]
    [InlineData("/subs/486/Krypto%20the%20Superdog%20-%20S02%20E05-E06%20-%20Growing%20Pains%20and%20K-9%20Crusader%20%28720p%20-%20AMZN%20Web-DL%29.html", "486", "Krypto the Superdog - S02 E05-E06 - Growing Pains and K-9 Crusader (720p - AMZN Web-DL)")]
    public void ParseSubcatUrlTest(string url, string? expectedId, string expectedTitle) {
        var (id, title) = SubcatClient.ParseSubcatUrl(url);
        Assert.Equal(expectedId, id);
        Assert.Equal(expectedTitle, title);
    }

    [Fact]
    public void GetSourcesPathTest() {
        Assert.Equal("/Sources/TV/Growing Pains/Season 3",
            SubcatClient.GetSourcesPath("/TV/Growing Pains/Season 3"));
    }

    [Fact]
    public void GetSubtitlePathTest() {
        var targetPath = SubcatClient.GetSubtitlePath("/TV/Growing Pains/Season 3",
            "https://www.subtitlecat.com/subs/1133/growing_pains_s03e02_aloha_2.html", "zh");

        Assert.Equal(
            "/Sources/TV/Growing Pains/Season 3/growing_pains_s03e02_aloha_2.1133.subcat.zh.srt",
            targetPath);
    }

    [Fact]
    public void GetSubtitlePathPercentEncodedTest() {
        var targetPath = SubcatClient.GetSubtitlePath("/TV/Growing Pains/Season 3",
            "https://www.subtitlecat.com/subs/1509/Life%20on%20Top%201x11%20-%20Growing%20Pains.html", "zh");

        Assert.Equal(
            "/Sources/TV/Growing Pains/Season 3/Life on Top 1x11 - Growing Pains.1509.subcat.zh.srt",
            targetPath);
    }
}
