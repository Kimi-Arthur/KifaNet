using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Kifa.Service;

namespace Kifa.Languages.Dwds;

public class DwdsGermanWord : DataModel, WithModelId {
    public static string ModelId => "dwds/words";

    public static KifaServiceClient<DwdsGermanWord> Client { get; set; } =
        new KifaServiceRestClient<DwdsGermanWord>();

    public HashSet<string> AudioLinks { get; set; } = new();
    public List<string> Etymology { get; set; } = new();

    public override DateTimeOffset? Fill() {
        var doc = new HtmlDocument();
        doc.LoadHtml(DwdsPage.Client.Get(Id)!.PageContent);

        AudioLinks = ExtractAudioLinks(doc);
        Etymology = ExtractEtymology(doc);

        return null;
    }

    static HashSet<string> ExtractAudioLinks(HtmlDocument doc) {
        var audioNodes = doc.DocumentNode.SelectNodes("//audio/source");

        return audioNodes == null
            ? new HashSet<string>()
            : audioNodes.Select(node => $"{node.Attributes["src"].Value}").ToHashSet();
    }

    static List<string> ExtractEtymology(HtmlDocument doc)
        => doc.DocumentNode.SelectNodes("//div[@class='dwdswb-ft-block']")
            ?.Where(nodePair => nodePair.ChildNodes.Count >= 2 &&
                                (nodePair.ChildNodes[0].InnerText == "Wortzerlegung" ||
                                 nodePair.ChildNodes[0].InnerText == "Grundform")).Select(nodePair
                => nodePair.ChildNodes[1].SelectNodes("./a").Select(node
                        => string.Join("",
                            node.ChildNodes.Where(n => !n.HasChildNodes).Select(n => n.InnerText)))
                    .ToList()).FirstOrDefault() ?? new List<string>();
}
