using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Languages.Goethe;

public class GoetheWordList : DataModel, WithModelId<GoetheWordList> {
    public static string ModelId => "Languages/goethe/lists";

    public static KifaServiceClient<GoetheWordList> Client { get; set; } =
        new KifaServiceRestClient<GoetheWordList>();

    public List<string> Words { get; set; } = new();
}
