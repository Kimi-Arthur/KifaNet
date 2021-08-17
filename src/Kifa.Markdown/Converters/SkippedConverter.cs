using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;

namespace Kifa.Markdown.Converters {
    public class SkippedConverter : HtmlMarkdownConverter {
        static HashSet<string> SkippedTags = new() {
            "head"
        };

        public override IEnumerable<MarkdownElement> ParseHtml(HtmlNode node) =>
            SkippedTags.Contains(node.Name)
                ? new[] {
                    new HtmlElement {
                        Html = ""
                    }
                }
                : Enumerable.Empty<MarkdownElement>();
    }
}
