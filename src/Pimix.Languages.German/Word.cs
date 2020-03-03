using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Languages.German {
    public enum WordType {
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

        public string Meaning { get; set; }

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
            Pronunciation = wiki.Pronunciation ?? pons.Pronunciation;
            PronunciationAudioLinkDuden = duden.PronunciationAudioLinkDuden;
            PronunciationAudioLinkWiktionary = wiki.PronunciationAudioLinkWiktionary;
            PronunciationAudioLinkPons = pons.PronunciationAudioLinkPons;

            Meaning = pons.Meaning;
            Type = pons.Type;
        }
    }

    public interface WordServiceClient : PimixServiceClient<Word> {
    }

    public class WordRestServiceClient : PimixServiceRestClient<Word>, WordServiceClient {
    }
}
