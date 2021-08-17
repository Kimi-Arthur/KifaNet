using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Kifa.Markdown.Elements;

namespace Kifa.Markdown.Converters {
    public abstract class HtmlMarkdownConverter {
        public static List<HtmlMarkdownConverter> Converters { get; set; } = new List<HtmlMarkdownConverter> {
            new HeadingConverter(),
            new LinkConverter(),
            new ParagraphConverter(),
            new SkippedConverter(),
            new NoopConverter(),
            new DefaultConverter()
        };

        public static IEnumerable<MarkdownElement> ParseAllHtml(IEnumerable<HtmlNode> nodes) =>
            nodes.SelectMany(node => Converters.Select(converter => converter.ParseHtml(node))
                .FirstOrDefault(elements => elements.Any()) ?? Enumerable.Empty<MarkdownElement>());

        public abstract IEnumerable<MarkdownElement> ParseHtml(HtmlNode node);
    }
}
