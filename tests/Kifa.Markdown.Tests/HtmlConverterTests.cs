using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using Xunit;

namespace Kifa.Markdown.Tests {
    public class HtmlConverterTests {
        static HttpClient client = new();

        [Theory]
        [InlineData("<h1>test</h1>", "# test\n\n")]
        [InlineData("<h4>test</h4>", "#### test\n\n")]
        [InlineData(
            "<p>A graphical icon widget drawn with a glyph from a font described in an <a href=\"widgets/IconData-class.html\">IconData</a> such as material's predefined <a href=\"widgets/IconData-class.html\">IconData</a>s in <a href=\"material/Icons-class.html\">Icons</a>.</p>",
            "A graphical icon widget drawn with a glyph from a font described in an [IconData](https://api.flutter.dev/flutter/widgets/IconData-class.html) such as material's predefined [IconData](https://api.flutter.dev/flutter/widgets/IconData-class.html)s in [Icons](https://api.flutter.dev/flutter/material/Icons-class.html).\n\n")]
        [InlineData("<ul>\n<li>abc</li><li>the</li><li>title\n<ul>\n<li>bcd</li><li>hh</li></ul></li></ul>",
            "- abc\n- the\n- title\n  - bcd\n  - hh\n")]
        [InlineData(
            "<p>For example, the level in <code>Debug.LogLevel.Default</code> overrides the level in <code>LogLevel.Default</code>.</p>",
            "For example, the level in `Debug.LogLevel.Default` overrides the level in `LogLevel.Default`.\n\n")]
        public void ParsingHtmlTest(string html, string markdown) {
            var parsed = HtmlMarkdownConverter.ParseAllHtml(new List<HtmlNode> {
                HtmlNode.CreateNode(html)
            }).ToList();

            Assert.Equal(markdown, parsed.Select(element => element.ToText()).JoinBy());
        }

        [Theory]
        [InlineData("https://api.flutter.dev/flutter/widgets/StatefulWidget-class.html",
            "//div[@id=\"dartdoc-main-content\"]", "stateful_widget.md")]
        [InlineData("https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=net-5.0", "//main",
            "file_stream.md")]
        public void ParsingDocumentTest(string url, string rootXpath, string outcomeFile) {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(client.GetStringAsync(url).Result);
            var markdownElements = HtmlMarkdownConverter
                .ParseAllHtml(new[] {htmlDocument.DocumentNode.SelectSingleNode(rootXpath)}).ToList();

            Assert.Equal(File.ReadAllText(outcomeFile), markdownElements.Select(element => element.ToText()).JoinBy());
        }
    }
}
