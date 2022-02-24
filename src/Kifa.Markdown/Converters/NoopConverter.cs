using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;

namespace Kifa.Markdown.Converters; 

public class NoopConverter : HtmlMarkdownConverter {
    static HashSet<string> NoopTags = new() {
        "section",
        "html",
        "body",
        "div",
        "span",
        "main" // From https://docs.microsoft.com/
    };

    public override IEnumerable<MarkdownElement> ParseHtml(HtmlNode node) =>
        NoopTags.Contains(node.Name) ? ParseAllHtml(node.ChildNodes) : Enumerable.Empty<MarkdownElement>();
}