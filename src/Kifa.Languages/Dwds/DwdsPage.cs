using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using HtmlAgilityPack;
using Kifa.Html;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Languages.Dwds;

public class DwdsPage : DataModel, WithModelId<DwdsPage> {
    public static string ModelId => "languages/dwds/pages";

    public static KifaServiceClient<DwdsPage> Client { get; set; } =
        new KifaServiceRestClient<DwdsPage>();

    const string PathPrefix = "/wb/";
    const string UrlPrefix = $"https://www.dwds.de{PathPrefix}";

    [JsonIgnore]
    public string Url => $"{UrlPrefix}{RealId}";

    public List<string> PagesBefore { get; set; } = [];
    public List<string> PagesAfter { get; set; } = [];

    [ExternalProperty("html")]
    public string PageContent { get; set; } = "";

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    static readonly HttpClient HttpClient = new();

    public override DateTimeOffset? Fill() {
        FillPageContent();
        FillNeighbouringPages();

        return DateTimeOffset.UtcNow + TimeSpan.FromDays(365);
    }

    void FillPageContent() {
        var response = HttpClient.SendWithRetry(Url);
        var actualUri = response.RequestMessage!.RequestUri!.ToString().Split('?')[0];
        if (!actualUri.StartsWith(UrlPrefix)) {
            throw new DataNotFoundException($"Redirected to an external/invalid URL: {actualUri}");
        }

        var actualId = HttpUtility.UrlDecode(actualUri[UrlPrefix.Length..]);

        if (actualId != RealId) {
            Client.Get(actualId);
            throw new DataIsLinkedException {
                TargetId = actualId
            };
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(response.GetString());

        var lemmaNode = doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'dwdswb-ft-lemmaansatz')]/b");
        if (lemmaNode != null) {
            var lemma = HttpUtility.UrlDecode(lemmaNode.InnerText.Trim());
            if (lemma != RealId) {
                Client.Get(lemma);
                throw new DataIsLinkedException {
                    TargetId = lemma
                };
            }
        }

        doc.DocumentNode
            .SelectNodes("//script | //p[. = 'Weitere Wörterbücher'] | " +
                         "//p[. = 'Weitere Wörterbücher']/following-sibling::*")
            ?.ForEach(n => n.Remove());

        PageContent = doc.DocumentNode.SelectSingleNode("//main").GetMinified();
    }

    void FillNeighbouringPages() {
        var doc = new HtmlDocument();
        doc.LoadHtml(PageContent);

        // Or 'alphabetisch nachfolgend'
        var nodes = doc.DocumentNode.SelectNodes("//th[. = 'alphabetisch vorangehend']/../..//td");
        if (nodes == null) {
            Logger.Warn("No neighbouring pages found.");
            return;
        }

        PagesBefore = nodes[0].SelectNodes(".//a").Select(SelectLink).ToList();
        PagesAfter = nodes[1].SelectNodes(".//a").Select(SelectLink).ToList();
    }

    static string SelectLink(HtmlNode node)
        => HttpUtility.UrlDecode(node.Attributes["href"].Value[PathPrefix.Length..]);
}
