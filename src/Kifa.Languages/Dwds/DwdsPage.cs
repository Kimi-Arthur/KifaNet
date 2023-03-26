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
    public static string ModelId => "dwds/pages";

    public static KifaServiceClient<DwdsPage> Client { get; set; } =
        new KifaServiceRestClient<DwdsPage>();

    const string PathPrefix = "/wb/";
    const string UrlPrefix = $"https://www.dwds.de{PathPrefix}";

    [JsonIgnore]
    public string Url => $"{UrlPrefix}{RealId}";

    public string? PageContent { get; set; }

    public List<string>? NeighbouringPages { get; set; }

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    static readonly HttpClient HttpClient = new();

    public override DateTimeOffset? Fill() {
        FillPageContent();
        FillNeighbouringPages();

        return DateTimeOffset.UtcNow + TimeSpan.FromDays(365);
    }

    void FillPageContent() {
        var response = HttpClient.SendWithRetry(Url);
        var actualId = response.RequestMessage!.RequestUri!.ToString()[UrlPrefix.Length..];

        if (actualId != RealId) {
            throw new DataNotFoundException("Redirected to an unknown page.");
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(response.GetString());

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
        var nodes = doc.DocumentNode.SelectNodes("//th[. = 'alphabetisch vorangehend']/../..//a");
        if (nodes == null) {
            Logger.Warn("No neibouring pages found.");
            return;
        }

        NeighbouringPages = nodes.Select(n
            => HttpUtility.UrlDecode(n.Attributes["href"].Value[PathPrefix.Length..])).ToList();
    }
}
