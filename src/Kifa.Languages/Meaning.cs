using System.Collections.Generic;

namespace Kifa.Languages;

public class TextWithTranslation {
    public virtual string Text { get; set; } = "";
    public virtual string Translation { get; set; } = "";
}

public class Meaning {
    public string Text { get; set; } = "";

    // TODO: Temp fix as we are running into issues of JSON deserialization of inherited fields.
    public virtual string Translation { get; set; } = "";

    public WordType? Type { get; set; }

    public List<TextWithTranslation> Examples { get; set; } = new();
}
