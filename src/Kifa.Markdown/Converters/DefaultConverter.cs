using System.Collections.Generic;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;

namespace Kifa.Markdown.Converters {
    public class DefaultConverter : HtmlMarkdownConverter {
        public override IEnumerable<MarkdownElement> ParseHtml(HtmlNode node) {
            yield return new HtmlElement {
                Html = node.OuterHtml
            };
        }
    }
}
