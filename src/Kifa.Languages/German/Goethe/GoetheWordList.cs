using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Languages.German.Goethe;

public class GoetheWordList : DataModel, WithModelId<GoetheWordList> {
    public static string ModelId => "goethe/lists";

    public static KifaServiceClient<GoetheWordList> Client { get; set; } =
        new KifaServiceRestClient<GoetheWordList>();

    public List<string> Words { get; set; } = new();
}
