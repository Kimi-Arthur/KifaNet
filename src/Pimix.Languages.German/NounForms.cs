using System.Collections.Generic;

namespace Pimix.Languages.German {
    public class NounForms : Dictionary<Case, Dictionary<Number, string>> {
    }

    public enum Gender {
        Masculine,
        Feminine,
        Neuter,
        Error
    }

    public enum Case {
        Nominative,
        Genitive,
        Dative,
        Accusative
    }

    public enum Number {
        Singular,
        Plural
    }
}
