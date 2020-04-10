using System.Collections.Generic;

namespace Pimix.Languages.German {
    public class VerbForms : Dictionary<VerbFormType, Dictionary<Person, string>> {
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
}
