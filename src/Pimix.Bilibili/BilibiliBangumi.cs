using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Bilibili {
    public class BilibiliBangumi : DataModel {
        public const string ModelId = "bilibili/bangumis";

        static BilibiliBangumiServiceClient client;

        public static BilibiliBangumiServiceClient Client => client =
            client ?? new BilibiliBangumiRestServiceClient();

        public string Name { get; set; }
        public List<string> Aids { get; set; }
    }

    public interface BilibiliBangumiServiceClient : PimixServiceClient<BilibiliBangumi> {
    }

    public class BilibiliBangumiRestServiceClient : PimixServiceRestClient<BilibiliBangumi>,
        BilibiliBangumiServiceClient {
    }
}
