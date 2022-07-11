using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;
using YamlDotNet.Serialization;

namespace Kifa.Languages.German.Goethe;

public class GoetheGermanWord : DataModel<GoetheGermanWord> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const string ModelId = "goethe/words";
    public override int CurrentVersion => 1;

    static readonly Regex RootWordPattern =
        new(@"^(das |der |die |\(.*\) |sich )?(.+?)(-$| \(.*\)| sein| gehen)?$");

    public string? Level { get; set; }
    public string? Form { get; set; }

    // A synonym text like: (CH) = (D, A) Hausmeister
    public string? Synonym { get; set; }

    // Only Word, Form are included.
    public GoetheGermanWord? Feminine { get; set; }

    // Only Word, Form, Feminine are included.
    public GoetheGermanWord? Abbreviation { get; set; }

    public string? Meaning { get; set; }

    public string? WikiMeanings { get; set; }

    public List<string>? Examples { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    public string RootWord => RootWordPattern.Match(Id).Groups[2].Value;

    public override DateTimeOffset? Fill() {
        var word = GermanWord.Client.Get(RootWord);

        if (word == null) {
            Logger.Warn($"Failed to find root word ({RootWord}) for {Id}.");
            return null;
        }

        Form = word.KeyForm;
        Meaning ??= word.Meaning;
        WikiMeanings = string.Join("; ", word.Meanings.Select(meaning => meaning.Translation));

        return null;
    }
}

public interface GoetheGermanWordServiceClient : KifaServiceClient<GoetheGermanWord> {
}

public class GoetheGermanWordRestServiceClient : KifaServiceRestClient<GoetheGermanWord>,
    GoetheGermanWordServiceClient {
}
