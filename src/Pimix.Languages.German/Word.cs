using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Languages.German {
    public enum WordType {
        Verb,
        Noun,
        Pronoun
    }

    public class Word : DataModel {
        public virtual WordType Type { get; set; }

        public string Translation { get; set; }

        public string Pronunciation { get; set; }

        [JsonIgnore]
        public string PronunciationAudioLink => PronunciationAudioLinkDuden ??
                                                PronunciationAudioLinkPons ?? PronunciationAudioLinkWiktionary;

        public string PronunciationAudioLinkDuden { get; set; }

        public string PronunciationAudioLinkPons { get; set; }

        public string PronunciationAudioLinkWiktionary { get; set; }
    }
}
