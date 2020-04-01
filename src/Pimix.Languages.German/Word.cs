using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Languages.German {
    public enum WordType {
        Unknown,
        Verb,
        Noun,
        Pronoun,
        Adjective,
        Adverb,
        Preposition
    }

    public class Word : DataModel {
        public const string ModelId = "languages/german/words";

        public virtual WordType Type { get; set; }

        public List<Meaning> Meanings { get; set; } = new List<Meaning>();

        public string Meaning => Meanings.First().Translation;

        public Breakdown Breakdown { get; set; }

        public string Pronunciation { get; set; }

        [JsonIgnore]
        public string PronunciationAudioLink => PronunciationAudioLinkDuden ?? PronunciationAudioLinkWiktionary
                                                ?? PronunciationAudioLinkPons;

        public string PronunciationAudioLinkDuden { get; set; }

        public string PronunciationAudioLinkPons { get; set; }

        public string PronunciationAudioLinkWiktionary { get; set; }

        public override void Fill() {
            var wiki = new DeWiktionaryClient().GetWord(Id);
            var pons = new PonsClient().GetWord(Id);
            var duden = new DudenClient().GetWord(Id);

            FillWithData(wiki, pons, duden);
        }

        protected void FillWithData(Word wiki, Word pons, Word duden) {
            Pronunciation = wiki.Pronunciation ?? pons.Pronunciation;
            PronunciationAudioLinkDuden = duden.PronunciationAudioLinkDuden;
            PronunciationAudioLinkWiktionary = wiki.PronunciationAudioLinkWiktionary;
            PronunciationAudioLinkPons = pons.PronunciationAudioLinkPons;

            Meanings = pons.Meanings;
            Type = pons.Type;
        }
    }

    public class Meaning {
        public string Translation { get; set; }
        public List<Example> Examples { get; set; }
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
