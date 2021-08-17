using System.Collections.Generic;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;
using NLog;

namespace Kifa.Markdown.Converters {
    public class DefaultConverter : HtmlMarkdownConverter {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public override IEnumerable<MarkdownElement> ParseHtml(HtmlNode node) {
            if (node.Name != "#text") {
                logger.Warn($"Unknown html node type ({node.Name}) in:\n{node.OuterHtml}");
            }

            yield return new HtmlElement {
                Html = node.OuterHtml
            };
        }
    }
}
