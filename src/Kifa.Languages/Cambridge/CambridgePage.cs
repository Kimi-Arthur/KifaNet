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

public class CambridgePage : DataModel {
    public const string ModelId = "cambridge/pages";

    public static CambridgePageServiceClient Client { get; set; } =
        new CambridgePageRestServiceClient();

    const string PathPrefix = "/dictionary/";
    const string UrlPrefix = $"https://dictionary.cambridge.org{PathPrefix}";

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
