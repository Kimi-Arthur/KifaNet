using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Kifa.Markdown.Converters;
using Xunit;

namespace Kifa.Markdown.Tests {
    public class HtmlConverterTests {
        [Theory]
        [InlineData("<h1>test</h1>", "# test\n\n")]
        [InlineData("<h4>test</h4>", "#### test\n\n")]
        [InlineData(
            "<p>A graphical icon widget drawn with a glyph from a font described in an <a href=\"widgets/IconData-class.html\">IconData</a> such as material's predefined <a href=\"widgets/IconData-class.html\">IconData</a>s in <a href=\"material/Icons-class.html\">Icons</a>.</p>",
            "A graphical icon widget drawn with a glyph from a font described in an [IconData](widgets/IconData-class.html) such as material's predefined [IconData](widgets/IconData-class.html)s in [Icons](material/Icons-class.html).\n\n")]
        public void ParsingTest(string html, string markdown) {
            var parsed = HtmlMarkdownConverter.ParseAllHtml(new List<HtmlNode> {
                HtmlNode.CreateNode(html)
            }).ToList();

            Assert.Equal(markdown, string.Join("", parsed.Select(element => element.ToText())));
        }
    }
}
