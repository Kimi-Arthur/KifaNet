using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;
using NLog;

namespace Kifa.Languages.German;

public class GermanWord : DataModel<GermanWord> {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public const string ModelId = "languages/german/words";

    public WordType? Type => Meanings.FirstOrDefault()?.Type ?? WordType.Unknown;

    public List<Meaning> Meanings { get; set; } = new();

    public string? Meaning => Meanings.FirstOrDefault()?.Translation;

    public Breakdown? Breakdown { get; set; }

    public string? Pronunciation { get; set; }

    public string? PronunciationAudioLink
        => (PronunciationAudioLinks.GetValueOrDefault(Source.Dwds) ??
            PronunciationAudioLinks.GetValueOrDefault(Source.Duden) ??
            PronunciationAudioLinks.GetValueOrDefault(Source.Wiktionary) ??
            PronunciationAudioLinks.GetValueOrDefault(Source.Pons))?.FirstOrDefault();

    public Dictionary<Source, HashSet<string>> PronunciationAudioLinks { get; set; } = new();

    // Shared for any meaning.
    public VerbForms VerbForms { get; set; } = new();

    public string? KeyForm
        => Type switch {
            WordType.Verb => GetKeyVerbForm(Id, VerbForms),
            WordType.Noun => GetSimplifiedPlural(Id, NounForms),
            _ => null
        };

    static string GetKeyVerbForm(string id, VerbForms verbForms) {
        if (!verbForms.ContainsKey(VerbFormType.IndicativePresent) ||
            !verbForms.ContainsKey(VerbFormType.IndicativePreterite) ||
            !verbForms.ContainsKey(VerbFormType.IndicativePerfect)) {
            return $"<{id}>";
        }

        return
            $"{verbForms[VerbFormType.IndicativePresent][Person.Er]}, {verbForms[VerbFormType.IndicativePreterite][Person.Er]}, {verbForms[VerbFormType.IndicativePerfect][Person.Er]}";
    }

    static Dictionary<char, char> UmlautMapping = new() {
        { 'a', 'ä' },
        { 'o', 'ö' },
        { 'u', 'ü' },
        { 'A', 'Ä' },
        { 'O', 'Ö' },
        { 'U', 'Ü' }
    };

    static string GetSimplifiedPlural(string original, NounForms nounForms) {
        if (!nounForms.ContainsKey(Case.Nominative)) {
            return $"<{original}>";
        }

        if (nounForms[Case.Nominative].TryGetValue(Number.Plural, out var plural)) {
            if (!nounForms[Case.Nominative].ContainsKey(Number.Singular)) {
                return "(Pl.)";
            }

            if (plural.StartsWith(original)) {
                // Add a suffix.
                return $"-{plural[original.Length..]}";
            }

            // Add a suffix and umlaut.
            var hasUmlaut = false;
            foreach (var (ochar, pchar) in original.Zip(plural)) {
                if (ochar != pchar) {
                    if (UmlautMapping.GetValueOrDefault(ochar) != pchar || hasUmlaut) {
                        // Only full text
                        return plural;
                    }

                    hasUmlaut = true;
                }
            }

            return !hasUmlaut ? plural : $"¨-{plural[original.Length..]}";
        }

        return "(Sg.)";
    }

    public Gender Gender { get; set; }

    public NounForms NounForms { get; set; } = new();

    public string GetNounFormWithArticle(Case formCase, Number formNumber)
        => NounForms.GetValueOrDefault(formCase, new Dictionary<Number, string>())
            .ContainsKey(formNumber)
            ? $"{GetArticle(Gender, formCase, formNumber)} {NounForms[formCase][formNumber]}"
            : "-";

    public static string GetArticle(Gender gender, Case formCase, Number formNumber)
        => formCase switch {
            Case.Nominative => formNumber switch {
                Number.Singular => gender switch {
                    Gender.Masculine => "der",
                    Gender.Feminine => "die",
                    Gender.Neuter => "das",
                    _ => null
                },
                Number.Plural => "die",
                _ => null
            },
            Case.Genitive => formNumber switch {
                Number.Singular => gender switch {
                    Gender.Masculine => "des",
                    Gender.Feminine => "der",
                    Gender.Neuter => "des",
                    _ => null
                },
                Number.Plural => "der",
                _ => null
            },
            Case.Dative => formNumber switch {
                Number.Singular => gender switch {
                    Gender.Masculine => "dem",
                    Gender.Feminine => "der",
                    Gender.Neuter => "dem",
                    _ => null
                },
                Number.Plural => "den",
                _ => null
            },
            Case.Accusative => formNumber switch {
                Number.Singular => gender switch {
                    Gender.Masculine => "den",
                    Gender.Feminine => "die",
                    Gender.Neuter => "das",
                    _ => null
                },
                Number.Plural => "die",
                _ => null
            },
            _ => null
        };

    protected (GermanWord wiki, GermanWord enWiki, GermanWord pons, GermanWord duden, GermanWord
        dwds) GetWords() {
        var wiki = new GermanWord();
        try {
            wiki = new DeWiktionaryClient().GetWord(Id);
        } catch (Exception ex) {
            logger.Warn(ex, $"Failed to get word from de.wiktionary.org for {Id}");
        }

        var enWiki = new GermanWord();
        try {
            enWiki = new EnWiktionaryClient().GetWord(Id);
        } catch (Exception ex) {
            logger.Warn(ex, $"Failed to get word from en.wiktionary.org for {Id}");
        }

        var pons = new GermanWord();
        try {
            pons = new PonsClient().GetWord(Id);
        } catch (Exception ex) {
            logger.Warn(ex, $"Failed to get pons word for {Id}");
        }

        var duden = new DudenClient().GetWord(Id);

        var dwds = new DwdsClient().GetWord(Id);

        return (wiki, enWiki, pons, duden, dwds);
    }

    public override DateTimeOffset? Fill() {
        FillWithData(GetWords());
        // Maybe some data will update.
        return DateTimeOffset.UtcNow + TimeSpan.FromDays(365);
    }

    protected void FillWithData(
        (GermanWord wiki, GermanWord enWiki, GermanWord pons, GermanWord duden, GermanWord dwds)
            words) {
        var (wiki, enWiki, pons, duden, dwds) = words;
        Pronunciation = wiki.Pronunciation ?? pons.Pronunciation;

        PronunciationAudioLinks[Source.Duden] =
            duden.PronunciationAudioLinks.GetValueOrDefault(Source.Duden, new HashSet<string>());
        PronunciationAudioLinks[Source.Wiktionary] =
            wiki.PronunciationAudioLinks.GetValueOrDefault(Source.Wiktionary,
                new HashSet<string>());
        PronunciationAudioLinks[Source.Pons] =
            pons.PronunciationAudioLinks.GetValueOrDefault(Source.Pons, new HashSet<string>());
        PronunciationAudioLinks[Source.Dwds] =
            dwds.PronunciationAudioLinks.GetValueOrDefault(Source.Dwds, new HashSet<string>());

        Meanings = enWiki.Meanings.Any() ? enWiki.Meanings : pons.Meanings;

        if (Meanings.Any(m => m.Type == WordType.Verb)) {
            VerbForms = words.wiki.VerbForms;
        }

        if (Meanings.Any(m => m.Type == WordType.Noun)) {
            Gender = words.wiki.Gender;
            NounForms = words.wiki.NounForms;
        }
    }
}

public class Meaning {
    public WordType Type { get; set; }
    public string? Translation { get; set; }
    public string? TranslationWithNotes { get; set; }
    public List<Example> Examples { get; set; } = new();
}

public class Breakdown {
    public List<Example> Segments { get; set; } = new();
}

public class Example {
    public string Text { get; set; } = "";
    public string Translation { get; set; } = "";
}

public interface GermanWordServiceClient : KifaServiceClient<GermanWord> {
}

public class GermanWordRestServiceClient : KifaServiceRestClient<GermanWord>,
    GermanWordServiceClient {
}
