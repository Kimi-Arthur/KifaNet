using System;
using Kifa.Service;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Languages.Kindle;

public class KindleLookup : DataModel, WithModelId<KindleLookup> {
    public static string ModelId => "kindle/lookups";

    public static KifaServiceClient<KindleLookup> Client { get; set; } =
        new KifaServiceRestClient<KindleLookup>();

    // Id will be <BOOK_HASH_KEY>/<LOCATION>
    // Examples: CR!PKA6SC8PQ17GDCW9SKX2RZ2TFD9W:AaUEAAAwAAAA:12970:13
    //          -> PKA6SC8PQ17GDCW9SKX2RZ2TFD9W/12970
    //           CR!E50026W9V11MB2BHTQKR511F3V96:CF5C2CC9:678626:8
    //          -> E50026W9V11MB2BHTQKR511F3V96_CF5C2CC9/678626
    // Note the two types of book ids.

    public string? BookHashKey => Id?.Split('/')[0];
    public int Location => Id == null ? 0 : int.Parse(Id.Split('/')[^1]);

    public string? Word { get; set; }

    public string? Usage { get; set; }

    public DateTimeOffset LookupTime { get; set; }
}
