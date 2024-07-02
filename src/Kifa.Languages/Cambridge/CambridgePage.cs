using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using Kifa.Html;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Languages.Cambridge;

public class CambridgePage : DataModel, WithModelId<CambridgePage> {
    public static string ModelId => "languages/cambridge/pages";

    public static KifaServiceClient<CambridgePage> Client { get; set; } =
        new KifaServiceRestClient<CambridgePage>();

    const string PathPrefix = "/dictionary/";
    const string UrlPrefix = $"https://dictionary.cambridge.org{PathPrefix}";

    [JsonIgnore]
    public string Url => $"{UrlPrefix}{RealId}";

    public string? PageContent { get; set; }

    public List<string> PagesBefore { get; set; } = [];
    public List<string> PagesAfter { get; set; } = [];

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    static readonly HttpClient HttpClient = new();

    public override DateTimeOffset? Fill() {
        FillPageContent();
        FillNeighbouringPages();

        return DateTimeOffset.UtcNow + TimeSpan.FromDays(365);
    }

    void FillPageContent() {
        var response = HttpClient.SendWithRetry(Url);
        var actualId =
            response.RequestMessage!.RequestUri!.ToString().RemoveAfter("?")[UrlPrefix.Length..];

        if (!IsValid(actualId)) {
            PageContent = "";
            return;
        }

        if (actualId != RealId) {
            Client.Get(actualId);
            throw new DataIsLinkedException {
                TargetId = actualId
            };
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(response.GetString());

        doc.DocumentNode.SelectNodes("//div[contains(@class, 'am-default')]")
            ?.ForEach(n => n.Remove());

        PageContent = doc.DocumentNode.SelectSingleNode("//article").GetMinified();
    }

    static bool IsValid(string id)
        => id.Split("/", StringSplitOptions.RemoveEmptyEntries).Length == 2;

    void FillNeighbouringPages() {
        var doc = new HtmlDocument();
        doc.LoadHtml(PageContent);
        var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'dbrowse')]//div");
        if (nodes == null) {
            Logger.Warn("No neighbouring pages found.");
            return;
        }

        var pages = nodes.Select(n => {
            var anchors = n.SelectNodes(".//a");
            return anchors?[0].Attributes["href"].Value[PathPrefix.Length..];
        }).ToList();
        var thisPageIndex = pages.IndexOf(null);
        PagesBefore = pages[..thisPageIndex].OnlyNonNull().ToList();
        PagesAfter = pages[(thisPageIndex + 1)..].OnlyNonNull().ToList();
    }
}
