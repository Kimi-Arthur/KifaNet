using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;

namespace Kifa.Markdown.Converters;

public class UnorderedListConverter : HtmlMarkdownConverter {
    int CurrentLevel;

    public override IEnumerable<MarkdownElement> ParseHtml(HtmlNode node) {
        if (node.Name == "ul") {
            CurrentLevel++;
            foreach (var childNode in node.ChildNodes) {
                if (childNode.Name == "li") {
                    yield return new UnorderedListItemElement {
                        Level = CurrentLevel - 1,
                        ChildElements = ParseAllHtml(childNode.ChildNodes).ToList()
                    };
                }
            }

            CurrentLevel--;
            if (CurrentLevel == 0) {
                yield return new LineBreakElement();
            }
        }
    }
}
