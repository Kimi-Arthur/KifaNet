using Pimix.Service;
using VerbForms = System.Collections.Generic.Dictionary<Pimix.Languages.German.VerbFormType, System.Collections.Generic.Dictionary<Pimix.Languages.German.Person, string>>;

namespace Pimix.Languages.German {
    public class Verb : Word {
        public new const string ModelId = "languages/german/verbs";

        public override WordType Type => WordType.Verb;

        public VerbForms VerbForms { get; set; } = new VerbForms();

        public override void Fill() {
            var wiki = new DeWiktionaryClient().GetWord(Id);
            var pons = new PonsClient().GetWord(Id) as Verb;
            var duden = new DudenClient().GetWord(Id);

            VerbForms = pons.VerbForms;
            Pronunciation = wiki.Pronunciation ?? pons.Pronunciation;
            PronunciationAudioLinkDuden = duden.PronunciationAudioLinkDuden;
            PronunciationAudioLinkWiktionary = wiki.PronunciationAudioLinkWiktionary;
            PronunciationAudioLinkPons = pons.PronunciationAudioLinkPons;
            Meaning = pons.Meaning;
        }
    }

    public enum VerbFormType {
        IndicativePresent
    }

    public enum Person {
        Ich,
        Du,
        Er,
        Wir,
        Ihr,
        Sie
    }

    public interface VerbServiceClient : PimixServiceClient<Verb> {
    }

    public class VerbRestServiceClient : PimixServiceRestClient<Verb>, VerbServiceClient {
    }
}
