using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Bilibili {
    public class BilibiliTv : DataModel {
        public const string ModelId = "bilibili/tvs";

        static BilibiliTvServiceClient client;

        public static BilibiliTvServiceClient Client => client =
            client ?? new BilibiliTvRestServiceClient();

        public string Name { get; set; }
        public List<string> Aids { get; set; }
    }

    public interface BilibiliTvServiceClient : PimixServiceClient<BilibiliTv> {
    }

    public class BilibiliTvRestServiceClient : PimixServiceRestClient<BilibiliTv>, BilibiliTvServiceClient {
    }
}
