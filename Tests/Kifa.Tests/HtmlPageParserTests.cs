using System.Linq;
using FluentAssertions;
using Kifa.Html;
using Xunit;

namespace Kifa.Tests;

public class HtmlPageParserTests {
    [Fact]
    public void PartiallyConsumedContainer_ReportsOnlyUnconsumedChildren() {
        var parser = new HtmlPageParser("""
            <div class="article">
              <div class="def">
                <span class="meaning">hello</span>
                <span class="extra">unparsed</span>
              </div>
              <div class="ignored">chrome</div>
            </div>
            """);

        var article = parser.Document.QuerySelector("div.article").Checked();
        parser.Consume(article.QuerySelector("span.meaning"));
        parser.Ignore("div.ignored", article);

        var unconsumed = parser.GetUnconsumedElements(article);
        unconsumed.Select(HtmlPageParser.DescribeElement).Should().Equal("span.extra");
    }

    [Fact]
    public void PartiallyConsumedContainer_ReportsResidualOwnText() {
        var parser = new HtmlPageParser("""
            <div class="article">
              <div class="phrasem">
                <span class="text">etw. im Griff haben</span>
                (umgangssprachlich)
              </div>
            </div>
            """);

        var article = parser.Document.QuerySelector("div.article").Checked();
        parser.Consume(article.QuerySelector("span.text"));

        var unconsumed = parser.GetUnconsumedElements(article);
        unconsumed.Select(HtmlPageParser.DescribeElement).Should().Equal("div.phrasem");

        // Marking own text consumed clears it once all children are also consumed.
        parser.ConsumeOwnText(article.QuerySelector("div.phrasem"));
        parser.GetUnconsumedElements(article).Should().BeEmpty();
    }

    [Fact]
    public void FullyUnconsumedSubtree_ReportsHighestContainerOnce() {
        var parser = new HtmlPageParser("""
            <div class="article">
              <div class="known">parsed</div>
              <div class="unknown-box"><p><span>nested</span> text</p></div>
            </div>
            """);

        var article = parser.Document.QuerySelector("div.article").Checked();
        parser.Consume(article.QuerySelector("div.known"));

        var unconsumed = parser.GetUnconsumedElements(article);
        unconsumed.Select(HtmlPageParser.DescribeElement).Should().Equal("div.unknown-box");
    }

    [Fact]
    public void IgnoreWhere_OnlyIgnoresMatchingElements() {
        var parser = new HtmlPageParser("""
            <div class="article">
              <p class="bg-info">Ignore me</p>
              <p class="bg-info">Keep me</p>
            </div>
            """);

        var article = parser.Document.QuerySelector("div.article").Checked();
        parser.IgnoreWhere("p.bg-info", el => el.TextContent.StartsWith("Ignore"), article);

        var unconsumed = parser.GetUnconsumedElements(article);
        unconsumed.Should().ContainSingle();
        unconsumed[0].TextContent.Should().Be("Keep me");
    }

    [Fact]
    public void TextlessElements_AreReportedUnlessExplicitlyIgnored() {
        var parser = new HtmlPageParser("""
            <div class="article">
              <div class="known">parsed</div>
              <div class="pic"><img src="a.jpg" alt="" /></div>
              <div class="freq-bar"></div>
              <br />
            </div>
            """);

        var article = parser.Document.QuerySelector("div.article").Checked();
        parser.Consume(article.QuerySelector("div.known"));
        parser.Ignore("br", article);

        var unconsumed = parser.GetUnconsumedElements(article);
        unconsumed.Select(HtmlPageParser.DescribeElement).Should().Equal("div.pic", "div.freq-bar");
    }

    [Fact]
    public void GetXPathAndSnippet_AnchorAtUniqueIdIncludingNumericAndTruncateSnippet() {
        var parser = new HtmlPageParser("""
            <html><body>
              <div id="1">
                <div id="d-1-1">
                  <div class="def"><span>ok</span></div>
                  <div class="extra"><b>first</b><b>second</b></div>
                </div>
                <p>header</p>
              </div>
            </body></html>
            """);

        var secondB = parser.Document.QuerySelectorAll("b")[1];
        parser.GetXPath(secondB).Should().Be("//*[@id='d-1-1']/div[2]/b[2]");
        HtmlPageParser.GetSnippet(secondB).Should().Be("<b>second</b>");
        HtmlPageParser.GetSnippet(secondB, 8).Should().Be("<b>seco…");

        var headerP = parser.Document.QuerySelector("p").Checked();
        parser.GetXPath(headerP).Should().Be("//*[@id='1']/p");
    }

    [Fact]
    public void GetXPath_AnchorClassesSupportSingleAndMultiClassElements() {
        var parser = new HtmlPageParser("""
            <html><body>
              <main>
                <div class="article"><div class="ft new-ft"><b>first</b></div></div>
                <div class="article"><div class="ft new-ft"><b>second</b></div></div>
              </main>
            </body></html>
            """, anchorClasses: ["ft"]);

        var bolds = parser.Document.QuerySelectorAll("b");
        parser.GetXPath(bolds[0]).Should().Be("(//div[@class='ft new-ft'])[1]/b");
        parser.GetXPath(bolds[1]).Should().Be("(//div[@class='ft new-ft'])[2]/b");

        var singleParser = new HtmlPageParser("""
            <html><body>
              <main><div class="article"><b>only</b></div></main>
            </body></html>
            """, anchorClasses: ["article"]);
        var onlyB = singleParser.Document.QuerySelector("b").Checked();
        singleParser.GetXPath(onlyB).Should().Be("//div[@class='article']/b");
    }
}
