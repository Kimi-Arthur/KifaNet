using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kifa.Languages.Cambridge;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;
using YamlDotNet.Serialization;

namespace Kifa.Languages.German.Goethe;

public class GoetheGermanWord : DataModel, WithModelId<GoetheGermanWord> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string ModelId => "goethe/words";

    public static KifaServiceClient<GoetheGermanWord> Client { get; set; } =
        new KifaServiceRestClient<GoetheGermanWord>();

    public override bool FillByDefault => true;
    public override int CurrentVersion => 3;

    static readonly Regex RootWordPattern =
        new(@"^(das |der |die |\(.*\) |sich |der/die )?(.+?)(-$| \(.*\)| sein| gehen)?$");

    public string? Level { get; set; }
    public string? Form { get; set; }
    public List<string> Usages { get; set; } = new();

    // A synonym text like: (CH) = (D, A) Hausmeister
    public string? Synonym { get; set; }

    // Only Word, Form are included.
    public GoetheGermanWord? Feminine { get; set; }

    // Only Word, Form, Feminine are included.
    public GoetheGermanWord? Abbreviation { get; set; }

    public string? Meaning { get; set; }

    public string? Cambridge { get; set; }

    public string? Wiki { get; set; }

    public List<string> Examples { get; set; } = new();

    [JsonIgnore]
    [YamlIgnore]
    public string RootWord => RootWordPattern.Match(Id).Groups[2].Value;

    public override DateTimeOffset? Fill() {
        var word = GermanWord.Client.Get(RootWord);

        if (word == null) {
            throw new UnableToFillException($"Failed to find root word ({RootWord}) for {Id}.");
        }

        Form ??= word.KeyForm;
        Meaning ??= word.Meaning;

        var cambridge = CambridgeGlobalGermanWord.Client.Get(RootWord);
        Cambridge = cambridge == null
            ? ""
            : string.Join("; ",
                cambridge.Entries
                    .SelectMany(e => e.Senses.Select(s => s.Definition?.Translation?.Trim()))
                    .ExceptNull().Where(x => x != "").Distinct()).Trim();

        Wiki = string.Join("; ", word.Meanings.Select(m => m.Translation)).Trim();

        return null;
    }
}
