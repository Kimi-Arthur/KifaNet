using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using AngleSharp;
using AngleSharp.Html;
using HtmlAgilityPack;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Languages.Dwds;

public class DwdsPage : DataModel {
    public const string ModelId = "dwds/pages";

    #region Clients

    public static ServiceClient Client { get; set; } = new RestServiceClient();

    public interface ServiceClient : KifaServiceClient<DwdsPage> {
    }

    public class RestServiceClient : KifaServiceRestClient<DwdsPage>, ServiceClient {
    }

    #endregion

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


        PageContent = BrowsingContext.New(Configuration.Default).OpenAsync(req
            => req.Content(doc.DocumentNode.SelectSingleNode("//main").OuterHtml)).Result.ToHtml(
            new MinifyMarkupFormatter {
            });
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
