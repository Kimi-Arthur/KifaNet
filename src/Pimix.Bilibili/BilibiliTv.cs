using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Bilibili {
    public class BilibiliTv : DataModel {
        public const string ModelId = "bilibili/tvs";

        static PimixServiceClient<BilibiliTv> client;

        public static PimixServiceClient<BilibiliTv> Client => client =
            client ?? new PimixServiceRestClient<BilibiliTv>();

        public string Name { get; set; }
        public List<string> Aids { get; set; }
    }
}
