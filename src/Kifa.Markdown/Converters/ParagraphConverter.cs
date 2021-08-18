using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;

namespace Kifa.Markdown.Converters {
    public class ParagraphConverter : HtmlMarkdownConverter {
        public override IEnumerable<MarkdownElement> ParseHtml(HtmlNode node) {
            if (node.Name == "p") {
                var childElements = ParseAllHtml(node.ChildNodes).ToList();
                if (childElements.Count > 0) {
                    yield return new ParagraphElement {
                        ChildElements = childElements
                    };
                }
            }
        }
    }
}
