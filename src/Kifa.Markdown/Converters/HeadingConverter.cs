using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;

namespace Kifa.Markdown.Converters {
    public class HeadingConverter : HtmlMarkdownConverter {
        static readonly Regex TagPattern = new Regex(@"h(\d+)");

        public override IEnumerable<MarkdownElement> ParseHtml(HtmlNode node) {
            var match = TagPattern.Match(node.Name);
            if (match.Success) {
                // TODO: may need to parse HTML.
                yield return new HeadingElement {
                    Level = int.Parse(match.Groups[1].Value),
                    Title = node.InnerText
                };
            }
        }
    }
}
