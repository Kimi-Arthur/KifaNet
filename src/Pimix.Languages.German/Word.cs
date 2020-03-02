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
        public string PronunciationAudioLink => PronunciationAudioLinkDuden ??
                                                PronunciationAudioLinkPons ?? PronunciationAudioLinkWiktionary;

        public string PronunciationAudioLinkDuden { get; set; }

        public string PronunciationAudioLinkPons { get; set; }

        public string PronunciationAudioLinkWiktionary { get; set; }

        public override void Fill() {
            var word = new PonsClient().GetWord(Id);
            Pronunciation = word.Pronunciation;
            Meaning = word.Meaning;
            Type = word.Type;
        }
    }

    public interface WordServiceClient : PimixServiceClient<Word> {
    }

    public class WordRestServiceClient : PimixServiceRestClient<Word>, WordServiceClient {
    }
}
