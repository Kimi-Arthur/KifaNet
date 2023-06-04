using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using Kifa.Memrise.Api;
using Kifa.Service;

namespace Kifa.Memrise;

public class MemriseLevel : DataModel, WithModelId<MemriseLevel> {
    public static string ModelId => "memrise/levels";

    public static KifaServiceClient<MemriseLevel> Client { get; set; } =
        new KifaServiceRestClient<MemriseLevel>();

    HttpClient? httpClient;

    HttpClient HttpClient {
        get {
            if (httpClient == null) {
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("cookie", MemriseClient.Cookies);
                httpClient.DefaultRequestHeaders.Add("x-csrftoken", MemriseClient.CsrfToken);
                httpClient.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
            }

            return httpClient;
        }
    }

    public string? Title { get; set; }
    public List<string> Words { get; set; } = new();

    public void FillLevel(string databaseUrl) {
        var rendered = HttpClient.Call(new GetLevelRpc(databaseUrl, Id))?.Rendered;

        if (rendered == null) {
            throw new Exception($"Failed to get current words in level {Id}.");
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(rendered);

        var node = doc.DocumentNode.SelectSingleNode("//h3[@class='level-name']");

        Title = node.InnerHtml.Trim();

        var nodes = doc.DocumentNode.SelectNodes("//tr[@data-thing-id]");
        Words.Clear();
        Words.AddRange(nodes.Select(n => n.Attributes["data-thing-id"].Value));
    }
}
