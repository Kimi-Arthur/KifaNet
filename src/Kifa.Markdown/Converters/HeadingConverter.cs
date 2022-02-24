using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;

namespace Kifa.Markdown.Converters;

public class HeadingConverter : HtmlMarkdownConverter {
    static readonly Regex TagPattern = new(@"h(\d+)");

    public override IEnumerable<MarkdownElement> ParseHtml(HtmlNode node) {
        var match = TagPattern.Match(node.Name);
        if (match.Success) {
            yield return new HeadingElement {
                Level = int.Parse(match.Groups[1].Value),
                TitleElements = ParseAllHtml(node.ChildNodes).ToList()
            };
        }
    }
}
