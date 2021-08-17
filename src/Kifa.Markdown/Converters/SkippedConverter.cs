using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;

namespace Kifa.Markdown.Converters {
    public class SkippedConverter : HtmlMarkdownConverter {
        static HashSet<string> SkippedTags = new() {
            "head"
        };

        static HashSet<string> SkippedIds = new() {
            "external-links"
        };

        public override IEnumerable<MarkdownElement> ParseHtml(HtmlNode node) =>
            SkippedTags.Contains(node.Name) || SkippedIds.Contains(node.Id)
                ? new[] {
                    new HtmlElement {
                        Html = ""
                    }
                }
                : Enumerable.Empty<MarkdownElement>();
    }
}
