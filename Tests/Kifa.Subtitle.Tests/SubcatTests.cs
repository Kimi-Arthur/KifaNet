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
        var link = SubcatClient.GetDownloadLink(doc, "en");
        Assert.NotNull(link);
        Assert.Equal("https://www.subtitlecat.com/subs/1436/TEST TEST TEST-en.srt",
            link.Value.Link);
        Assert.False(link.Value.NeedsGeneration);
    }

    [Fact]
    public void GetDownloadLinkNonExistingRequestedLanguageTest() {
        var doc = SampleHtml.GetDocument();
        var link = SubcatClient.GetDownloadLink(doc, "zh");
        Assert.NotNull(link);
        Assert.Equal("https://www.subtitlecat.com/subs/1436/TEST TEST TEST-orig.srt",
            link.Value.Link);
        Assert.True(link.Value.NeedsGeneration);
    }

    [Fact]
    public void GetDownloadLinkNotFoundTest() {
        var doc = SampleHtml.GetDocument();
        var link = SubcatClient.GetDownloadLink(doc, "fr");
        Assert.Null(link);
    }

    [Fact]
    public void GetDownloadLinksTest() {
        var doc = SampleHtml.GetDocument();
        var links = SubcatClient.GetDownloadLinks(doc, ["en", "zh", "fr"]);
        Assert.Equal(2, links.Count);
        Assert.False(links["en"].NeedsGeneration);
        Assert.Equal("https://www.subtitlecat.com/subs/1436/TEST TEST TEST-en.srt",
            links["en"].Link);
        Assert.True(links["zh"].NeedsGeneration);
        Assert.Equal("https://www.subtitlecat.com/subs/1436/TEST TEST TEST-orig.srt",
            links["zh"].Link);
    }

    [Theory]
    [InlineData("https://www.subtitlecat.com/subs/1133/growing_pains_s03e02_aloha_2.html", "1133",
        "growing_pains_s03e02_aloha_2")]
    [InlineData("/subs/1436/TEST_TEST_TEST-orig.srt", "1436", "TEST_TEST_TEST")]
    [InlineData("subs/1133/growing_pains_s03e02_aloha_2.html", "1133",
        "growing_pains_s03e02_aloha_2")]
    [InlineData("https://www.subtitlecat.com/index.php", null, "index")]
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
}
