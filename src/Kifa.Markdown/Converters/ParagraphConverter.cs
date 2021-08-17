using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;

namespace Kifa.Markdown.Converters {
    public class ParagraphConverter : HtmlMarkdownConverter {
        public override IEnumerable<MarkdownElement> ParseHtml(HtmlNode node) {
            if (node.Name == "p") {
                yield return new ParagraphElement {
                    ChildElements = ParseAllHtml(node.ChildNodes).ToList()
                };
            }
        }
    }
}
