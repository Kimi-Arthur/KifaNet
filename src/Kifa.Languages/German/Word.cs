using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Languages.German {
    public class Word : DataModel {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public const string ModelId = "languages/german/words";

        public WordType Type => Meanings.First().Type;

        public List<Meaning> Meanings { get; set; } = new List<Meaning>();

        public string Meaning => Meanings.FirstOrDefault()?.Translation;

        public Breakdown Breakdown { get; set; }

        public string Pronunciation { get; set; }

        public string PronunciationAudioLink =>
            (PronunciationAudioLinks.GetValueOrDefault(Source.Dwds) ??
             PronunciationAudioLinks.GetValueOrDefault(Source.Duden) ??
             PronunciationAudioLinks.GetValueOrDefault(Source.Wiktionary) ??
             PronunciationAudioLinks.GetValueOrDefault(Source.Pons))?.FirstOrDefault();

        public Dictionary<Source, List<string>> PronunciationAudioLinks { get; set; } = new();

        // Shared for any meaning.
        public VerbForms VerbForms { get; set; } = new VerbForms();

        [JsonIgnore]
        public List<string> KeyVerbForms =>
            new() {
                VerbForms[VerbFormType.IndicativePresent][Person.Er],
                VerbForms[VerbFormType.IndicativePreterite][Person.Er],
                VerbForms[VerbFormType.IndicativePerfect][Person.Er]
            };

        public Gender Gender { get; set; }

        public NounForms NounForms { get; set; } = new NounForms();

        public string GetNounFormWithArticle(Case formCase, Number formNumber) =>
            NounForms.GetValueOrDefault(formCase, new Dictionary<Number, string>()).ContainsKey(formNumber)
                ? $"{GetArticle(Gender, formCase, formNumber)} {NounForms[formCase][formNumber]}"
                : "-";

        public static string GetArticle(Gender gender, Case formCase, Number formNumber) =>
            formCase switch {
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

        protected (Word wiki, Word enWiki, Word pons, Word duden, Word dwds) GetWords() {
            var wiki = new Word();
            try {
                wiki = new DeWiktionaryClient().GetWord(Id);
            } catch (Exception ex) {
                logger.Warn(ex, $"Failed to get word from de.wiktionary.org for {Id}");
            }

            var enWiki = new Word();
            try {
                enWiki = new EnWiktionaryClient().GetWord(Id);
            } catch (Exception ex) {
                logger.Warn(ex, $"Failed to get word from en.wiktionary.org for {Id}");
            }

            var pons = new Word();
            try {
                pons = new PonsClient().GetWord(Id);
            } catch (Exception ex) {
                logger.Warn(ex, $"Failed to get pons word for {Id}");
            }

            var duden = new DudenClient().GetWord(Id);

            var dwds = new DwdsClient().GetWord(Id);

            return (wiki, enWiki, pons, duden, dwds);
        }

        public override bool? Fill() {
            FillWithData(GetWords());
            return false;
        }

        protected void FillWithData((Word wiki, Word enWiki, Word pons, Word duden, Word dwds) words) {
            var (wiki, enWiki, pons, duden, dwds) = words;
            Pronunciation = wiki.Pronunciation ?? pons.Pronunciation;

            PronunciationAudioLinks[Source.Duden] = duden.PronunciationAudioLinks.GetValueOrDefault(Source.Duden);
            PronunciationAudioLinks[Source.Wiktionary] =
                wiki.PronunciationAudioLinks.GetValueOrDefault(Source.Wiktionary);
            PronunciationAudioLinks[Source.Pons] = pons.PronunciationAudioLinks.GetValueOrDefault(Source.Pons);
            PronunciationAudioLinks[Source.Dwds] = dwds.PronunciationAudioLinks.GetValueOrDefault(Source.Dwds);

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
        public string Title { get; set; }
        public WordType Type { get; set; }
        public string Translation { get; set; }
        public string TranslationWithNotes { get; set; }
        public List<Example> Examples { get; set; } = new List<Example>();
    }

    public class Breakdown {
        public List<Example> Segments { get; set; }
    }

    public class Example {
        public string Text { get; set; }
        public string Translation { get; set; }
    }

    public interface WordServiceClient : KifaServiceClient<Word> {
    }

    public class WordRestServiceClient : KifaServiceRestClient<Word>, WordServiceClient {
    }
}
