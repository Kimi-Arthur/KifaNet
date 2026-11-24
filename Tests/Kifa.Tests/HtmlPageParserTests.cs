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
}
