using System.Collections.Generic;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Languages;

public class Example {
    public string Text { get; set; } = "";
    public string Translation { get; set; } = "";
}

public class Meaning {
    public WordType Type { get; set; }
    public string? Translation { get; set; }
    public string? TranslationWithNotes { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    public string? SafeTranslation
        => string.IsNullOrEmpty(Translation) ? TranslationWithNotes : Translation;

    public List<Example> Examples { get; set; } = new();
}
