using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;

namespace Kifa.Languages.German;

public class DwdsClient {
    static HttpClient dwdsClient = new();

    public static GermanWordServiceClient GermanWordClient { get; set; } =
        new GermanWordRestServiceClient();

    public GermanWord GetWord(string wordId) {
        var doc = new HtmlDocument();
        using var response = dwdsClient.GetAsync($"https://www.dwds.de/wb/{wordId}").Result;
        doc.LoadHtml(response.GetString());
        var audioNodes = doc.DocumentNode.SelectNodes("//audio/source");

        var word = new GermanWord();

        if (audioNodes?.Count > 0) {
            word.PronunciationAudioLinks ??= new Dictionary<Source, HashSet<string>>();
            word.PronunciationAudioLinks[Source.Dwds] = audioNodes
                .Select(audioNode => $"{audioNode.Attributes["src"].Value}").ToHashSet();
        }

        var segments = ExtractSourceWords(doc);

        if (segments != null) {
            word.Breakdown = new Breakdown {
                Segments = segments
            };
        }

        return word;
    }

    List<Example>? ExtractSourceWords(HtmlDocument doc) {
        var nodes = doc.DocumentNode.SelectNodes("//div[@class='dwdswb-ft-block']");
        foreach (var nodePair in nodes) {
            if (nodePair.ChildNodes.Count < 2) {
                continue;
            }

            if (nodePair.ChildNodes[0].InnerText == "Wortzerlegung" ||
                nodePair.ChildNodes[0].InnerText == "Grundform") {
                return nodePair.ChildNodes[1].SelectNodes("./a").Select(node
                        => GetTranslated(string.Join("",
                            node.ChildNodes.Where(n => !n.HasChildNodes).Select(n => n.InnerText))))
                    .ToList();
            }
        }

        return null;
    }

    Example GetTranslated(string german) {
        var word = GermanWordClient.Get(german);
        return new Example {
            Text = german,
            Translation = word.Meaning
        };
    }
}
