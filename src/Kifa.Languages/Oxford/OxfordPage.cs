using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Kifa.Html;
using Kifa.Service;
using NLog;

namespace Kifa.Languages.Oxford;

public class OxfordPage : DataModel, WithModelId<OxfordPage> {
    public static string ModelId => "languages/oald/pages";

    public static KifaServiceClient<OxfordPage> Client { get; set; } =
        new KifaServiceRestClient<OxfordPage>();

    public List<string> PagesBefore { get; set; } = [];
    public List<string> PagesAfter { get; set; } = [];

    [ExternalProperty("html")]
    public string PageContent { get; set; } = "";

    const string UrlPrefix = "https://www.oxfordlearnersdictionaries.com/definition/english/";

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    static readonly HttpClient HttpClient = new();

    public override DateTimeOffset? Fill() {
        var response = HttpClient.SendWithRetry(UrlPrefix + Id);
        var responseId = GetId(response.RequestMessage.Checked().RequestUri.Checked().ToString());
        if (responseId != RealId) {
            throw new DataNotFoundException("Redirected to an unknown page.");
        }

        var doc = response.GetString().GetDocument();

        FillPageContent(doc);
        FillNeighbouringPages(doc);

        return null;
    }

    static readonly string[] UnwantedSelectors = ["#ring-links-box", ".am-entry_long"];

    static readonly Regex ClumsySpace = new("\n +");

    void FillPageContent(IDocument doc) {
        var element = doc.QuerySelector(".entry").Checked();
        foreach (var selector in UnwantedSelectors) {
            foreach (var e in element.QuerySelectorAll(selector)) {
                Logger.Trace($"Remove element matching {selector}: {e.OuterHtml}");
                e.Remove();
            }
        }

        foreach (var titleElement in doc.QuerySelectorAll("[title]")) {
            titleElement.SetAttribute("title",
                ClumsySpace.Replace(titleElement.GetAttribute("title").Checked(), _ => " "));
        }

        PageContent = element.GetMinified();
    }

    void FillNeighbouringPages(IDocument doc) {
        PagesBefore.Clear();
        PagesAfter.Clear();
        bool pastCurrentWord = false;
        foreach (var id in doc.QuerySelectorAll(".nearby a")
                     .Select(e => GetId(e.GetAttribute("href").Checked()))) {
            if (pastCurrentWord) {
                PagesAfter.Add(id);
            } else if (id == RealId) {
                pastCurrentWord = true;
            } else {
                PagesBefore.Add(id);
            }
        }
    }

    static string GetId(string url) => url[UrlPrefix.Length..];
}
