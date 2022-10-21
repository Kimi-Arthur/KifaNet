using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AngleSharp;
using AngleSharp.Html;
using HtmlAgilityPack;
using Kifa.Service;
using NLog;

namespace Kifa.Languages.Cambridge;

public class CambridgePage : DataModel {
    public const string ModelId = "cambridge/pages";

    const string PathPrefix = "/dictionary/";
    const string UrlPrefix = $"https://dictionary.cambridge.org{PathPrefix}";

    public string Url => $"{UrlPrefix}{Id}";

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
        var content = HttpClient.GetStringAsync(Url).Result;
        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        doc.DocumentNode.SelectNodes("//div[contains(@class, 'am-default')]")
            ?.ForEach(n => n.Remove());

        PageContent = BrowsingContext.New(Configuration.Default).OpenAsync(req
            => req.Content(doc.DocumentNode.SelectSingleNode("//article").OuterHtml)).Result.ToHtml(
            new MinifyMarkupFormatter {
            });
    }

    void FillNeighbouringPages() {
        var doc = new HtmlDocument();
        doc.LoadHtml(PageContent);
        var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'dbrowse')]//a");
        if (nodes == null) {
            Logger.Warn("No neibouring pages found.");
            return;
        }

        NeighbouringPages =
            nodes.Select(n => n.Attributes["href"].Value[PathPrefix.Length..]).ToList();
    }
}

public interface CambridgePageServiceClient : KifaServiceClient<CambridgePage> {
}

public class CambridgePageRestServiceClient : KifaServiceRestClient<CambridgePage>,
    CambridgePageServiceClient {
}
