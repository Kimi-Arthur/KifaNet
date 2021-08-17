using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Kifa.Markdown.Converters;
using Kifa.Markdown.Elements;

namespace Kifa.Markdown {
    public abstract class HtmlMarkdownConverter {
        public static List<HtmlMarkdownConverter> Converters { get; set; } = new List<HtmlMarkdownConverter> {
            new HeadingConverter(),
            new LinkConverter(),
            new ParagraphConverter(),
            new UnorderedListConverter(),
            new InlineCodeConverter(),

            // Order of converters above doesn't matter.
            new SkippedConverter(),
            new NoopConverter(),
            new DefaultConverter()
        };

        public static IEnumerable<MarkdownElement> ParseAllHtml(IEnumerable<HtmlNode> nodes) {
            foreach (var node in nodes) {
                foreach (var converter in Converters) {
                    var elements = converter.ParseHtml(node).ToList();
                    if (elements.Count > 0) {
                        foreach (var element in elements) {
                            yield return element;
                        }

                        break;
                    }
                }
            }
        }

        // TODO: Dummy implementation
        public static string ResolveUrl(string url) {
            if (url.StartsWith("http")) {
                return url;
            }

            return $"https://api.flutter.dev/flutter/{url}";
        }

        public abstract IEnumerable<MarkdownElement> ParseHtml(HtmlNode node);
    }
}
