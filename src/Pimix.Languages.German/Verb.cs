using System.Runtime.Serialization;
using Pimix.Service;
using VerbForms = System.Collections.Generic.Dictionary<Pimix.Languages.German.VerbFormType, System.Collections.Generic.Dictionary<Pimix.Languages.German.Person, string>>;

namespace Pimix.Languages.German {
    public class Verb : Word {
        public const string ModelId = "languages/german/verbs";

        public override WordType Type => WordType.Verb;

        public VerbForms VerbForms { get; set; } = new VerbForms();

        public override void Fill() {
            var word = new PonsClient().GetWord(Id) as Verb;
            VerbForms = word.VerbForms;
            Pronunciation = word.Pronunciation;
            Translation = word.Translation;
        }
    }

    public enum VerbFormType {
        PresentIndicative
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
