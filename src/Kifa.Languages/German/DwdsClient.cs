using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;

namespace Kifa.Languages.German;

public class DwdsClient {
    static HttpClient dwdsClient = new();

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

        word.Etymology = ExtractEtymology(doc);

        return word;
    }

    static List<string>? ExtractEtymology(HtmlDocument doc)
        => doc.DocumentNode.SelectNodes("//div[@class='dwdswb-ft-block']")
            ?.Where(nodePair => nodePair.ChildNodes.Count >= 2 &&
                                (nodePair.ChildNodes[0].InnerText == "Wortzerlegung" ||
                                 nodePair.ChildNodes[0].InnerText == "Grundform")).Select(nodePair
                => nodePair.ChildNodes[1].SelectNodes("./a").Select(node
                        => string.Join("",
                            node.ChildNodes.Where(n => !n.HasChildNodes).Select(n => n.InnerText)))
                    .ToList()).FirstOrDefault();
}
