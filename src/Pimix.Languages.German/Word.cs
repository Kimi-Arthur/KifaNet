using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NLog;
using Pimix.Service;

namespace Pimix.Languages.German {
    public class Word : DataModel {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public const string ModelId = "languages/german/words";

        [JsonIgnore]
        public WordType Type => Meanings.First().Type;

        public List<Meaning> Meanings { get; set; } = new List<Meaning>();

        [JsonIgnore]
        public string Meaning => Meanings.FirstOrDefault()?.Translation;

        public Breakdown Breakdown { get; set; }

        public string Pronunciation { get; set; }

        [JsonIgnore]
        public string PronunciationAudioLink => PronunciationAudioLinkDuden ?? PronunciationAudioLinkWiktionary
            ?? PronunciationAudioLinkPons;

        public string PronunciationAudioLinkDuden { get; set; }

        public string PronunciationAudioLinkPons { get; set; }

        public string PronunciationAudioLinkWiktionary { get; set; }

        // Shared for any meaning.
        public VerbForms VerbForms { get; set; } = new VerbForms();

        public Gender Gender { get; set; }

        public NounForms NounForms { get; set; } = new NounForms();

        protected (Word wiki, Word enWiki, Word pons, Word duden) GetWords() {
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

            return (wiki, enWiki, pons, duden);
        }

        public override void Fill() {
            FillWithData(GetWords());
        }

        protected void FillWithData((Word wiki, Word enWiki, Word pons, Word duden) words) {
            var (wiki, enWiki, pons, duden) = words;
            Pronunciation = wiki.Pronunciation ?? pons.Pronunciation;
            PronunciationAudioLinkDuden = duden.PronunciationAudioLinkDuden;
            PronunciationAudioLinkWiktionary = wiki.PronunciationAudioLinkWiktionary;
            PronunciationAudioLinkPons = pons.PronunciationAudioLinkPons;

            Meanings = enWiki.Meanings.Any() ? enWiki.Meanings : pons.Meanings;
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

    public interface WordServiceClient : PimixServiceClient<Word> {
    }

    public class WordRestServiceClient : PimixServiceRestClient<Word>, WordServiceClient {
    }
}
