using System.Linq;
using System.Web;
using HtmlAgilityPack;
using MwParserFromScratch.Nodes;

namespace Kifa.Languages.German; 

public static class WikiExtensions {
    public static string GetTitle(this Heading heading) => heading.Inlines.First().ToPlainText();

    public static string InnerTextTrimmed(this HtmlNode node) => HttpUtility.HtmlDecode(node.InnerText).Trim();

    public static string InnerHtmlTrimmed(this HtmlNode node) => HttpUtility.HtmlDecode(node.InnerHtml).Trim();
}