using System.Collections.Generic;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;

namespace Kifa.Markdown.Converters;

public class InlineCodeConverter : HtmlMarkdownConverter {
    public override IEnumerable<MarkdownElement> ParseHtml(HtmlNode node) {
        if (node.Name == "code") {
            yield return new InlineCodeElement {
                Code = node.InnerText
            };
        }
    }
}
