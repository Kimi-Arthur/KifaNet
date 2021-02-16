using System.Collections.Generic;

namespace Kifa.Languages.German {
    public class VerbForms : Dictionary<VerbFormType, Dictionary<Person, string>> {
    }

    public enum VerbFormType {
        IndicativePresent,
        IndicativePreterite,
        IndicativePerfect,
        Imperative
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
