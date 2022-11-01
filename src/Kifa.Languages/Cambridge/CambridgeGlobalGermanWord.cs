using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp;
using AngleSharp.Dom;
using Kifa.Service;

namespace Kifa.Languages.Cambridge;

// Entries in GLOBAL German–English Dictionary from https://dictionary.cambridge.org/dictionary/german-english/
public class CambridgeGlobalGermanWord : DataModel {
    public const string ModelId = "cambridge/german_words";

    public static CambridgeGlobalGermanWordServiceClient Client { get; set; } =
        new CambridgeGlobalGermanWordRestServiceClient();

    public List<CambridgeGlobalGermanEntry> Entries { get; set; }

    const string PagePrefix = "german-english";

    public override DateTimeOffset? Fill() {
        var page = CambridgePage.Client.Get($"{PagePrefix}/{Id}");
        if (page == null) {
            throw new UnableToFillException($"Raw page not found {PagePrefix}/{Id}.");
        }

        var document = BrowsingContext.New(Configuration.Default).OpenAsync(req
            => req.Content(page.PageContent)).Result;
        var root = document.GetElementsByClassName("dictionary")
            .FirstOrDefault(e => e.QuerySelector("#dataset_k-de-en-global") != null);

        if (root == null) {
            throw new UnableToFillException("No GLOBAL element found on page.");
        }

        var heads = root.GetElementsByClassName("normal-entry");
        var bodies = root.GetElementsByClassName("normal-entry-body");

        if (heads.Length != bodies.Length) {
            throw new UnableToFillException($"The normal-entry count {heads.Length} and " +
                                            $"normal-entry-body count {bodies.Length}" +
                                            "don't match unexpectedly.");
        }

        Entries = new List<CambridgeGlobalGermanEntry>();

        for (var i = 0; i < heads.Length; i++) {
            var head = heads[i];
            var headWord = head.QuerySelector(".di-title").SafeText();
            if (headWord == Id) {
                var entry = new CambridgeGlobalGermanEntry();
                entry.WordType = GetWordType(head.QuerySelector(".pos").SafeText(),
                    head.GetElementsByClassName("gram").Select(e => e.SafeText()));
                entry.Senses = bodies[i].GetElementsByClassName("sense-body")
                    .Select(CambridgeGlobalGermanSense.FromElement).ToList();
                Entries.Add(entry);
            }
        }

        return Date.Zero;
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
    #region public late WordType Type { get; set; }

    WordType? wordType;

    public WordType WordType {
        get => Late.Get(wordType);
        set => Late.Set(ref wordType, value);
    }

    #endregion

    public List<CambridgeGlobalGermanSense>? Senses { get; set; }
}

public class CambridgeGlobalGermanSense {
    public CambridgeGlobalGermanDefinition? Definition { get; set; }

    public List<CambridgeGlobalGermanPhrase>? Phrases { get; set; }

    public static CambridgeGlobalGermanSense FromElement(IElement element) {
        return new CambridgeGlobalGermanSense {
            Definition =
                CambridgeGlobalGermanDefinition.FromElement(element
                    .GetElementsByClassName("def-block").Single()),
            Phrases = element.GetElementsByClassName("phrase-block")
                .Select(CambridgeGlobalGermanPhrase.FromElement).ToList()
        };
    }
}

public class CambridgeGlobalGermanDefinition : Meaning {
    public string? Notes { get; set; }

    public CambridgeGlobalGermanDefinition FillFromElement(IElement element) {
        var defHead = element.QuerySelector(".def-head > .def").SafeText();
        return this;
    }

    public static CambridgeGlobalGermanDefinition FromElement(IElement element)
        => new CambridgeGlobalGermanDefinition().FillFromElement(element);
}

public class CambridgeGlobalGermanPhrase : CambridgeGlobalGermanDefinition {
    public string? Phrase { get; set; }

    public CambridgeGlobalGermanPhrase FillFromElement(IElement element) {
        base.FillFromElement(element);
        Phrase = element.QuerySelector(".phrase").SafeText();
        return this;
    }


    public static CambridgeGlobalGermanPhrase FromElement(IElement element)
        => new CambridgeGlobalGermanPhrase().FillFromElement(element);
}

public interface
    CambridgeGlobalGermanWordServiceClient : KifaServiceClient<CambridgeGlobalGermanWord> {
}

public class CambridgeGlobalGermanWordRestServiceClient :
    KifaServiceRestClient<CambridgeGlobalGermanWord>, CambridgeGlobalGermanWordServiceClient {
}

static class ElementExtensions {
    public static string SafeText(this INode? element)
        => element == null ? "" : element.TextContent.Trim();
}
