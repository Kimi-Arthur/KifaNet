using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Bilibili {
    public class BilibiliUploader : DataModel {
        public const string ModelId = "bilibili/uploaders";

        static PimixServiceClient<BilibiliUploader> client;

        public static PimixServiceClient<BilibiliUploader> Client => client ??= new PimixServiceRestClient<BilibiliUploader>();

        public string Name { get; set; }
        public List<string> Aids { get; set; }
    }
}
