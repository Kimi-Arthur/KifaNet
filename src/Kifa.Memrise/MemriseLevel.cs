using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using Kifa.Memrise.Api;
using Kifa.Service;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Memrise;

public class MemriseLevel : DataModel, WithModelId<MemriseLevel> {
    public static string ModelId => "memrise/levels";

    public override bool FillByDefault => true;

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

    [JsonIgnore]
    [YamlIgnore]
    string DatabaseUrl => MemriseCourse.Client.Get(Id.Split("/")[0]).Checked().DatabaseUrl;

    [JsonIgnore]
    [YamlIgnore]
    string LevelId => Id.Split("/")[1];

    public string? Title { get; set; }
    public List<string> Words { get; set; } = new();

    public override DateTimeOffset? Fill() {
        var rendered = HttpClient.Call(new GetLevelRpc(DatabaseUrl, LevelId)).Rendered;

        if (rendered == null) {
            throw new Exception($"Failed to get current words in level {LevelId}.");
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(rendered);

        var node = doc.DocumentNode.SelectSingleNode("//h3[@class='level-name']");

        Title = node.InnerHtml.Trim();

        var nodes = doc.DocumentNode.SelectNodes("//tr[@data-thing-id]");
        Words.Clear();
        if (nodes != null) {
            Words.AddRange(nodes.Select(n => n.Attributes["data-thing-id"].Value));
        }

        return null;
    }
}
