using System.Collections.Generic;

namespace Kifa.Languages;

public class TextWithTranslation {
    public virtual string Text { get; set; } = "";
    public virtual string Translation { get; set; } = "";
}

public class Meaning : TextWithTranslation {
    public WordType? Type { get; set; }

    public List<TextWithTranslation> Examples { get; set; } = new();
}
