using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp;
using Kifa.Service;
using NLog;

namespace Kifa.Languages.Cambridge;

// Entries in GLOBAL German–English Dictionary from https://dictionary.cambridge.org/dictionary/german-english/
public class CambridgeGlobalGermanWord : DataModel {
    public const string ModelId = "cambridge/german_words";

    public static CambridgeGermanWordServiceClient Client { get; set; } =
        new CambridgeGermanWordRestServiceClient();

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public List<CambridgeGlobalGermanEntry> Entries { get; set; } = new();

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

        for (var i = 0; i < heads.Length; i++) {
            var head = heads[i];
            var headWord = head.GetElementsByClassName("di-title")[0].TextContent.Trim();
            if (headWord == Id) {
                var entry = new CambridgeGlobalGermanEntry();
                entry.WordType =
                    GetWordType(head.GetElementsByClassName("pos")[0].TextContent.Trim(),
                        head.GetElementsByClassName("gram").Select(e => e.TextContent.Trim()));
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

    WordType? type;

    public WordType WordType {
        get => Late.Get(type);
        set => Late.Set(ref type, value);
    }

    #endregion
}

public interface CambridgeGermanWordServiceClient : KifaServiceClient<CambridgeGlobalGermanWord> {
}

public class CambridgeGermanWordRestServiceClient :
    KifaServiceRestClient<CambridgeGlobalGermanWord>, CambridgeGermanWordServiceClient {
}
