using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using HtmlAgilityPack;

namespace Kifa.Html;

public static class HtmlExtensions {
    public static IDocument GetDocument(this string content)
        => BrowsingContext.New(Configuration.Default).OpenAsync(req => req.Content(content)).Result;

    public static string GetMinified(this string html)
        => html.GetDocument().ToHtml(new MinifyMarkupFormatter());

    public static string GetMinified(this HtmlNode htmlNode) => htmlNode.OuterHtml.GetMinified();
}
