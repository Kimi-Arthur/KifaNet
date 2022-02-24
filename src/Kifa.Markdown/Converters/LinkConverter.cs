using System.Collections.Generic;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;

namespace Kifa.Markdown.Converters;

public class LinkConverter : HtmlMarkdownConverter {
    public override IEnumerable<MarkdownElement> ParseHtml(HtmlNode node) {
        // Link overrides for local pages can be modified later.
        if (node.Name == "a" && !string.IsNullOrWhiteSpace(node.InnerText)) {
            yield return new LinkElement {
                Text = node.InnerText,
                Target = ResolveUrl(node.GetAttributeValue("href", ""))
            };
        }
    }
}
