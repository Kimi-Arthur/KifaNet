using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kifa.Service;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Languages.German.Goethe;

public class GoetheGermanWord : DataModel<GoetheGermanWord> {
    public const string ModelId = "goethe/words";

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
    public List<string>? Examples { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    public string RootWord => RootWordPattern.Match(Id).Groups[2].Value;

    public override DateTimeOffset? Fill() {
        if (Examples != null && !Examples[0].StartsWith("example")) {
            // Still need to refresh next time.
            return Date.Zero;
        }

        if (Form != null && Form != "¨-e" && Form != "") {
            return null;
        }

        var word = new GermanWord {
            Id = RootWord
        };

        word.Fill();

        Form = word.KeyForm;
        Meaning ??= word.Meaning;

        return null;
    }
}

public interface GoetheGermanWordServiceClient : KifaServiceClient<GoetheGermanWord> {
}

public class GoetheGermanWordRestServiceClient : KifaServiceRestClient<GoetheGermanWord>,
    GoetheGermanWordServiceClient {
}
