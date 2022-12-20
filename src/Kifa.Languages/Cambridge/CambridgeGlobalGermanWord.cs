using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Kifa.Html;
using Kifa.Service;
using NLog;

namespace Kifa.Languages.Cambridge;

// Entries in GLOBAL German–English Dictionary from https://dictionary.cambridge.org/dictionary/german-english/
public class CambridgeGlobalGermanWord : DataModel, WithModelId {
    public static string ModelId => "cambridge/german";

    public override int CurrentVersion => 1;

    #region Clients

    public static ServiceClient Client { get; set; } = new RestServiceClient();

    public interface ServiceClient : KifaServiceClient<CambridgeGlobalGermanWord> {
    }

    public class
        RestServiceClient : KifaServiceRestClient<CambridgeGlobalGermanWord>, ServiceClient {
    }

    #endregion

    public List<CambridgeGlobalGermanEntry> Entries { get; set; } = new();

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    const string PagePrefix = "german-english";

    public override DateTimeOffset? Fill() {
        var page = CambridgePage.Client.Get($"{PagePrefix}/{Id}");
        if (page?.PageContent == null) {
            Logger.Error($"Raw page not found {PagePrefix}/{Id}.");
            return DateTimeOffset.Now + TimeSpan.FromDays(365);
        }

        var document = page.PageContent.GetDocument();
        var root = document.GetElementsByClassName("dictionary")
            .FirstOrDefault(e => e.QuerySelector("#dataset_k-de-en-global") != null);

        if (root == null) {
            Logger.Error("No GLOBAL element found on page.");
            return DateTimeOffset.Now + TimeSpan.FromDays(365);
        }

        var heads = root.GetElementsByClassName("normal-entry");
        var bodies = root.GetElementsByClassName("normal-entry-body");

        if (heads.Length != bodies.Length) {
            throw new UnableToFillException($"The normal-entry count {heads.Length} and " +
                                            $"normal-entry-body count {bodies.Length}" +
                                            "don't match unexpectedly.");
        }

        Entries = heads.Zip(bodies, (head, body) => (head, body))
            .Where(entry => entry.head.QuerySelector(".di-title").GetSafeInnerText() == Id).Select(
                entry => new CambridgeGlobalGermanEntry {
                    WordType = GetWordType(entry.head.QuerySelector(".pos").GetSafeInnerText(),
                        entry.head.GetElementsByClassName("gram")
                            .Select(e => e.GetSafeInnerText())),
                    Senses = entry.body.GetElementsByClassName("sense-body")
                        .Select(CambridgeGlobalGermanSense.FromElement).ToList()
                }).ToList();

        return DateTimeOffset.Now + TimeSpan.FromDays(365);
    }

    static WordType GetWordType(string text, IEnumerable<string> notes) {
        if (notes.Any(n => n.Contains("pronoun"))) {
            return WordType.Pronoun;
        }

        return text switch {
            "determiner" => WordType.Article,
            _ => Enum.TryParse<WordType>(text, true, out var result) ? result : WordType.Unknown
        };
    }
}

public class CambridgeGlobalGermanEntry {
    #region public late WordType WordType { get; set; }

    WordType? wordType;

    public WordType WordType {
        get => Late.Get(wordType);
        set => Late.Set(ref wordType, value);
    }

    #endregion

    public List<CambridgeGlobalGermanSense> Senses { get; set; } = new();
}

public class CambridgeGlobalGermanSense {
    public CambridgeGlobalGermanDefinition? Definition { get; set; }

    public List<CambridgeGlobalGermanPhrase> Phrases { get; set; } = new();

    public static CambridgeGlobalGermanSense FromElement(IElement element) {
        var definitionElement =
            element.Children.FirstOrDefault(e => e.ClassList.Contains("def-block"));

        if (definitionElement != null) {
            return new CambridgeGlobalGermanSense {
                Definition = CambridgeGlobalGermanDefinition.FromElement(definitionElement),
                Phrases = element.QuerySelectorAll(".phrase-block")
                    .Select(CambridgeGlobalGermanPhrase.FromElement).ToList(),
            };
        }

        return new CambridgeGlobalGermanSense {
            Phrases = element.QuerySelectorAll(".phrase-block")
                .Select(CambridgeGlobalGermanPhrase.FromElement).ToList()
        };
    }
}

public class CambridgeGlobalGermanDefinition {
    public string? Meaning { get; set; }

    public string? Translation { get; set; }

    public List<TextWithTranslation> Examples { get; set; } = new();

    public string Notes { get; set; } = "";

    public Dictionary<string, string> CrossReferences { get; set; } = new();

    public CambridgeGlobalGermanDefinition FillFromElement(IElement element) {
        if (!element.ClassList.Contains("def-block")) {
            throw new ArgumentException(
                $"{nameof(element)} should be an element of class `def-block`.");
        }

        Meaning = element.QuerySelector(".def-head > .def").GetSafeInnerText();
        var notesElement = element.QuerySelector(".def-head > .def-info");
        if (notesElement != null) {
            notesElement.GetElementsByClassName("freq").ForEach(e => e.Remove());
            Notes = notesElement.GetSafeInnerText().Trim();
        }

        Translation = string.Join("",
            element.QuerySelectorAll(".def-body > .trans").Select(e => e.GetSafeInnerText()));
        Examples = element.QuerySelectorAll(".examp").Select(div => new TextWithTranslation {
            Text = div.QuerySelector(".eg").GetSafeInnerText(),
            Translation = div.QuerySelector(".trans").GetSafeInnerText()
        }).ToList();

        CrossReferences = element.Children.Where(e => e.ClassList.Contains("xref"))
            .ToDictionary(x => x.ClassList[1], x => x.QuerySelector(".x-h").GetSafeInnerText());

        return this;
    }

    public static CambridgeGlobalGermanDefinition FromElement(IElement element)
        => new CambridgeGlobalGermanDefinition().FillFromElement(element);
}

public class CambridgeGlobalGermanPhrase {
    public string? Phrase { get; set; }

    public List<CambridgeGlobalGermanDefinition> Definitions { get; set; } = new();

    public CambridgeGlobalGermanPhrase FillFromElement(IElement element) {
        Definitions = element.QuerySelectorAll(".phrase-body > .def-block")
            .Select(e => CambridgeGlobalGermanDefinition.FromElement(e)).ToList();
        Phrase = element.QuerySelector(".phrase").GetSafeInnerText();
        return this;
    }


    public static CambridgeGlobalGermanPhrase FromElement(IElement element)
        => new CambridgeGlobalGermanPhrase().FillFromElement(element);
}

static class ElementExtensions {
    public static string GetSafeInnerText(this INode? element)
        => element == null ? "" : element.TextContent;
}
