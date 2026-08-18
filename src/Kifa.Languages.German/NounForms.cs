using System.Collections.Generic;

namespace Kifa.Languages.German;

public class NounForms : Dictionary<Case, Dictionary<Number, string?>> {
}

public enum Gender {
    Masculine,
    Feminine,
    Neuter
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
