using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Languages.English.Reading;

// Word encountered in reading English books.
public class ReadingEnglishWord {
    // We will put different record for different meanings of the same word.
    public string Word { get; set; }

    public WordType Type { get; set; }

    public string Meaning { get; set; }

    // Text used in test. Should include both type and meaning.
    [JsonIgnore]
    [YamlIgnore]
    public string MeaningText => $"({Type.GetShort()}) {Meaning}";

    public string Pronunciation { get; set; }

    public ReadingContext Context { get; set; }
}

public class ReadingContext {
    // TODO: Could be an id later when we have that dataset.
    public string Book { get; set; }

    // TODO: Could be a structured data describing the location.
    public string Location { get; set; }

    // The sentence that contains the word.
    // It's easier to remember with the actual text encountered.
    public string Quote { get; set; }
}
