using System;
using System.IO;
using Kifa.Html;
using Kifa.Subtitle.Subcat;
using Xunit;

namespace Kifa.Subtitle.Tests;

public class SubcatTests {
    static readonly string SampleSubtitlePageHtml =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Subcat", "subtitle.html"));

    static readonly string SampleSearchPageHtml =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Subcat", "search.html"));

    [Fact]
    public void SubcatChoiceToStringWithSourceAndCountsTest() {
        var choice = new SubcatChoice {
            OriginalLink =
                SubcatClient.GetFullUrl("/subs/1133/growing_pains_s03e02_aloha_2-orig.srt"),
            Preview = "Hi, welcome to Growing Pains.\nToday we are in Hawaii!",
            SourceLanguage = "English",
            Size = "25 KB",
            DownloadCount = 2,
            LanguageCount = 2
        };
        Assert.Equal(
            "growing_pains_s03e02_aloha_2 (from English) (25 KB, 2 downloads): https://www.subtitlecat.com/subs/1133/growing_pains_s03e02_aloha_2-orig.srt\n\tHi, welcome to Growing Pains.\n\tToday we are in Hawaii!",
            choice.ToString());
    }

    [Fact]
    public void GetSrtPreviewTest() {
        var srt = "1\n00:00:01,000 --> 00:00:04,000\nHello World\n\n2\n00:00:05,000 --> 00:00:08,000\nSecond Line";
        var preview = SubcatClient.GetSrtPreview(srt);
        Assert.Equal("Hello World\nSecond Line", preview);
    }

    [Fact]
    public void GetDownloadUrlNotFoundTest() {
        var doc = SampleSubtitlePageHtml.GetDocument();
        var downloadUrl = SubcatClient.GetDownloadUrl(doc, "fr");
        Assert.Null(downloadUrl);
    }

    [Fact]
    public void GetDownloadUrlsTest() {
        var doc = SampleSubtitlePageHtml.GetDocument();
        var urls = SubcatClient.GetDownloadUrls(doc, ["en", "zh", "fr"]);
        Assert.Equal(2, urls.Count);
        Assert.Equal("/subs/1499/growing_pains_s03e02_aloha_2-en.srt", urls["en"]);
        Assert.Null(urls["zh"]);
        Assert.False(urls.ContainsKey("fr"));
    }

    [Fact]
    public void ParseSearchResultsTest() {
        var doc = SampleSearchPageHtml.GetDocument();
        var results = SubcatClient.ParseSearchResults(doc);

        Assert.Equal(2, results.Count);

        var first = results[0];
        Assert.Equal("subs/1133/growing_pains_s03e02_aloha_2.html", first.Link);
        Assert.Equal("subs/1133/growing_pains_s03e02_aloha_2-orig.srt", first.OriginalLink);
        Assert.Equal("English", first.SourceLanguage);
        Assert.Equal("25 KB", first.Size);
        Assert.Equal(2, first.DownloadCount);
        Assert.Equal(2, first.LanguageCount);

        var second = results[1];
        Assert.Equal("subs/1133/Growing%20Pains%20s03e02%20Aloha%202.html", second.Link);
        Assert.Equal("subs/1133/Growing%20Pains%20s03e02%20Aloha%202-orig.srt",
            second.OriginalLink);
        Assert.Equal("English", second.SourceLanguage);
        Assert.Equal("24 KB", second.Size);
        Assert.Equal(1, second.DownloadCount);
        Assert.Equal(1, second.LanguageCount);
    }

    [Theory]
    [InlineData("https://www.subtitlecat.com/subs/1133/growing_pains_s03e02_aloha_2.html", "1133",
        "growing_pains_s03e02_aloha_2")]
    [InlineData("/subs/1436/When_Calls_the_Heart_S13E05-orig.srt", "1436",
        "When_Calls_the_Heart_S13E05")]
    [InlineData("/subs/1436/When_Calls_the_Heart_S13E05-en.srt", "1436",
        "When_Calls_the_Heart_S13E05")]
    [InlineData("/subs/1436/When_Calls_the_Heart_S13E05-zh-CN.srt", "1436",
        "When_Calls_the_Heart_S13E05")]
    [InlineData("subs/1133/growing_pains_s03e02_aloha_2.html", "1133",
        "growing_pains_s03e02_aloha_2")]
    [InlineData("https://www.subtitlecat.com/index.php", null, "index")]
    [InlineData(
        "https://www.subtitlecat.com/subs/1509/Life%20on%20Top%201x11%20-%20Growing%20Pains.html",
        "1509", "Life on Top 1x11 - Growing Pains")]
    [InlineData(
        "/subs/486/Krypto%20the%20Superdog%20-%20S02%20E05-E06%20-%20Growing%20Pains%20and%20K-9%20Crusader%20%28720p%20-%20AMZN%20Web-DL%29.html",
        "486",
        "Krypto the Superdog - S02 E05-E06 - Growing Pains and K-9 Crusader (720p - AMZN Web-DL)")]
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
            "https://www.subtitlecat.com/subs/1509/Life%20on%20Top%201x11%20-%20Growing%20Pains.html",
            "zh");

        Assert.Equal(
            "/Sources/TV/Growing Pains/Season 3/Life on Top 1x11 - Growing Pains.1509.subcat.zh.srt",
            targetPath);
    }

    [Fact]
    public void GetSubtitleFileNameTest() {
        var fileName = SubcatClient.GetSubtitleFileName(
            "https://www.subtitlecat.com/subs/1133/growing_pains_s03e02_aloha_2-zh.srt", "zh");
        Assert.Equal("growing_pains_s03e02_aloha_2.1133.subcat.zh.srt", fileName);
    }

    [Fact]
    public void GetSubtitleFileNameOrigTest() {
        var fileName = SubcatClient.GetSubtitleFileName(
            "https://www.subtitlecat.com/subs/1133/growing_pains_s03e02_aloha_2-orig.srt", "orig");
        Assert.Equal("growing_pains_s03e02_aloha_2.1133.subcat.orig.srt", fileName);
    }

    [Fact]
    public void GetSubtitlePathOrigTest() {
        var targetPath = SubcatClient.GetSubtitlePath("/TV/Growing Pains/Season 3",
            "https://www.subtitlecat.com/subs/1133/growing_pains_s03e02_aloha_2-orig.srt", "orig");

        Assert.Equal(
            "/Sources/TV/Growing Pains/Season 3/growing_pains_s03e02_aloha_2.1133.subcat.orig.srt",
            targetPath);
    }
}
