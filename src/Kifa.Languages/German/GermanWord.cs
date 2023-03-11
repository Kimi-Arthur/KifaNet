using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Languages.Dwds;
using Kifa.Service;
using NLog;

namespace Kifa.Languages.German;

public class GermanWord : DataModel, WithModelId {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string ModelId => "languages/german/words";

    public override int CurrentVersion => 13;

    public static KifaServiceClient<GermanWord> Client { get; set; } =
        new KifaServiceRestClient<GermanWord>();

    public WordType? Type { get; set; }

    public List<Meaning> Meanings { get; set; } = new();

    public string? Meaning { get; set; }

    public List<string> Etymology { get; set; } = new();

    public string? Pronunciation { get; set; }

    public string? PronunciationAudioLink
        => PronunciationAudioLinks == null
            ? null
            : (PronunciationAudioLinks.GetValueOrDefault(Source.Dwds) ??
               PronunciationAudioLinks.GetValueOrDefault(Source.Duden) ??
               PronunciationAudioLinks.GetValueOrDefault(Source.Wiktionary) ??
               PronunciationAudioLinks.GetValueOrDefault(Source.Pons))?.FirstOrDefault();

    public Dictionary<Source, HashSet<string>>? PronunciationAudioLinks { get; set; }

    public HashSet<string>? Images { get; set; }

    // Shared for any meaning.
    public VerbForms? VerbForms { get; set; }

    public Gender? Gender { get; set; }

    public NounForms? NounForms { get; set; }

    public string? KeyForm
        => Type switch {
            WordType.Verb => VerbForms == null ? null : GetKeyVerbForm(Id, VerbForms),
            WordType.Noun => NounForms == null ? null : GetSimplifiedPlural(Id, NounForms),
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

    static readonly Dictionary<char, char> UmlautMapping = new() {
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
            var umlaut = ' ';
            foreach (var (ochar, pchar) in original.Zip(plural)) {
                if (ochar != pchar) {
                    if (UmlautMapping.GetValueOrDefault(ochar) != pchar || umlaut != ' ') {
                        // Only full text in this special case
                        return plural;
                    }

                    umlaut = pchar;
                }
            }

            return umlaut == ' ' ? plural : $"{umlaut}-{plural[original.Length..]}";
        }

        return "(Sg.)";
    }

    public string GetNounFormWithArticle(Case formCase, Number formNumber)
        => NounForms!.GetValueOrDefault(formCase, new Dictionary<Number, string>())
            .ContainsKey(formNumber)
            ? $"{GetArticle(Gender, formCase, formNumber)} {NounForms[formCase][formNumber]}"
            : "-";

    public static string? GetArticle(Gender? gender, Case formCase, Number formNumber)
        => formCase switch {
            Case.Nominative => formNumber switch {
                Number.Singular => gender switch {
                    German.Gender.Masculine => "der",
                    German.Gender.Feminine => "die",
                    German.Gender.Neuter => "das",
                    _ => null
                },
                Number.Plural => "die",
                _ => null
            },
            Case.Genitive => formNumber switch {
                Number.Singular => gender switch {
                    German.Gender.Masculine => "des",
                    German.Gender.Feminine => "der",
                    German.Gender.Neuter => "des",
                    _ => null
                },
                Number.Plural => "der",
                _ => null
            },
            Case.Dative => formNumber switch {
                Number.Singular => gender switch {
                    German.Gender.Masculine => "dem",
                    German.Gender.Feminine => "der",
                    German.Gender.Neuter => "dem",
                    _ => null
                },
                Number.Plural => "den",
                _ => null
            },
            Case.Accusative => formNumber switch {
                Number.Singular => gender switch {
                    German.Gender.Masculine => "den",
                    German.Gender.Feminine => "die",
                    German.Gender.Neuter => "das",
                    _ => null
                },
                Number.Plural => "die",
                _ => null
            },
            _ => null
        };

    protected (GermanWord wiki, GermanWord enWiki, GermanWord duden, DwdsGermanWord? dwds)
        GetWords() {
        var wiki = new GermanWord();
        try {
            wiki = new DeWiktionaryClient().GetWord(Id);
        } catch (Exception ex) {
            Logger.Warn(ex, $"Failed to get word from de.wiktionary.org for {Id}");
        }

        var enWiki = new GermanWord();
        try {
            enWiki = new EnWiktionaryClient().GetWord(Id);
        } catch (Exception ex) {
            Logger.Warn(ex, $"Failed to get word from en.wiktionary.org for {Id}");
        }

        var duden = new DudenClient().GetWord(Id);

        var dwds = DwdsGermanWord.Client.Get(Id);

        return (wiki, enWiki, duden, dwds);
    }

    public override DateTimeOffset? Fill() {
        FillWithData(GetWords());
        // Maybe some data will update.
        return DateTimeOffset.UtcNow + TimeSpan.FromDays(365);
    }

    protected void FillWithData(
        (GermanWord wiki, GermanWord enWiki, GermanWord duden, DwdsGermanWord? dwds) words) {
        var (wiki, enWiki, duden, dwds) = words;
        Pronunciation = wiki.Pronunciation;

        PronunciationAudioLinks = new List<GermanWord> {
                duden,
                wiki
            }.Select(word => word.PronunciationAudioLinks).ExceptNull()
            .SelectMany(pronunciationLinks => pronunciationLinks)
            .ToDictionary(item => item.Key, item => item.Value);

        if (dwds?.AudioLinks is { Count: > 0 }) {
            PronunciationAudioLinks[Source.Dwds] = dwds.AudioLinks;
        }

        Meanings = enWiki.Meanings;

        Meaning ??= Meanings?.FirstOrDefault()?.Translation;
        Type ??= Meanings?.FirstOrDefault()?.Type ?? WordType.Unknown;

        if (Meanings?.Any(m => m.Type == WordType.Verb) == true) {
            VerbForms = words.wiki.VerbForms;
        } else {
            VerbForms = null;
        }

        if (Meanings?.Any(m => m.Type == WordType.Noun) == true) {
            Gender = words.wiki.Gender;
            NounForms = words.wiki.NounForms;
        } else {
            Gender = null;
            NounForms = null;
        }

        Etymology ??= dwds.Etymology;
    }

    public IEnumerable<string> GetTopPronunciationAudioLinks()
        => PronunciationAudioLinks == null
            ? Enumerable.Empty<string>()
            : PronunciationAudioLinks.GetValueOrDefault(Source.Dwds, new HashSet<string>())
                .Concat(PronunciationAudioLinks.GetValueOrDefault(Source.Duden,
                    new HashSet<string>())).Concat(PronunciationAudioLinks.GetValueOrDefault(
                    Source.Wiktionary, new HashSet<string>()));
}
