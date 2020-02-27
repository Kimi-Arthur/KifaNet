using VerbForms = System.Collections.Generic.Dictionary<Pimix.Languages.German.VerbFormType, System.Collections.Generic.Dictionary<Pimix.Languages.German.Person, string>>;

namespace Pimix.Languages.German {
    public class Verb : Word {
        public const string ModelId = "languages/german/verbs";

        public WordType Type => WordType.Verb;

        public VerbForms VerbForms { get; set; } = new VerbForms();

        public override void Fill() {
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
}
