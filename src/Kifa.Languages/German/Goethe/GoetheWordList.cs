using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Languages.German.Goethe {
    public class GoetheWordList : DataModel<GoetheWordList> {
        public const string ModelId = "goethe/lists";

        public List<string> Words { get; set; }
    }

    public interface GoetheWordListServiceClient : KifaServiceClient<GoetheWordList> {
    }

    public class GoetheWordListRestServiceClient : KifaServiceRestClient<GoetheWordList>, GoetheWordListServiceClient {
    }
}
