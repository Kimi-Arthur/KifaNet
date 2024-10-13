using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using HtmlAgilityPack;

namespace Kifa.Html;

public static class HtmlExtensions {
    public static IDocument GetDocument(this string content)
        => BrowsingContext.New(Configuration.Default).OpenAsync(req => req.Content(content)).Result;

    public static string GetMinified(this IMarkupFormattable html)
        => html.ToHtml(new MinifyMarkupFormatter());

    public static string GetMinified(this string html) => html.GetDocument().GetMinified();

    public static string GetMinified(this HtmlNode htmlNode) => htmlNode.OuterHtml.GetMinified();

    public static string GetPrettified(this IMarkupFormattable html)
        => html.ToHtml(new PrettyMarkupFormatter());

    public static string GetPrettified(this string html) => html.GetDocument().GetPrettified();

    public static string GetPrettified(this HtmlNode htmlNode) => htmlNode.OuterHtml.GetPrettified();
}
