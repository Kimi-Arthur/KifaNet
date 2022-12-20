using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Languages.German.Goethe;

public class GoetheWordList : DataModel, WithModelId {
    public static string ModelId => "goethe/lists";

    public List<string> Words { get; set; } = new();
}

public interface GoetheWordListServiceClient : KifaServiceClient<GoetheWordList> {
}

public class GoetheWordListRestServiceClient : KifaServiceRestClient<GoetheWordList>,
    GoetheWordListServiceClient {
}
