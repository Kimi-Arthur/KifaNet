using System.Collections.Generic;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;
using NLog;

namespace Kifa.Markdown.Converters;

public class DefaultConverter : HtmlMarkdownConverter {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override IEnumerable<MarkdownElement> ParseHtml(HtmlNode node) {
        var html = node.OuterHtml;

        if (node.Name != "#text") {
            Logger.Warn($"Unknown html node type ({node.Name}) in:\n{html}");
        }

        if (string.IsNullOrWhiteSpace(node.InnerText)) {
            yield break;
        }

        if (char.IsWhiteSpace(html[0])) {
            html = $" {html.TrimStart()}";
        }

        if (char.IsWhiteSpace(html[^1])) {
            html = $"{html.TrimEnd()} ";
        }

        yield return new HtmlElement {
            Html = html
        };
    }
}
