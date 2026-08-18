using System.Collections.Generic;

namespace Kifa.Languages.German;

public class AdjectiveForms : Dictionary<AdjectiveFormType, string?> {
}

public enum AdjectiveFormType {
    Positiv,
    Komparativ,
    Superlativ
}
