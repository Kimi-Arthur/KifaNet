using System;
using Kifa.Service;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Languages.Kindle;

public class KindleLookup : DataModel, WithModelId<KindleLookup> {
    public static string ModelId => "kindle/lookups";

    public static KifaServiceClient<KindleLookup> Client { get; set; } =
        new KifaServiceRestClient<KindleLookup>();

    // Id will be book_name/byte_location
    [JsonIgnore]
    [YamlIgnore]
    public string? BookName { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    public int Location { get; set; }

    public string? Word { get; set; }

    public string? Usage { get; set; }

    public DateTimeOffset LookupTime { get; set; }

    // Extra fields to add after import. We will skip complicated fields like form, examples etc.
    public string? Meaning { get; set; }

    public string? Pronunciation { get; set; }
}
