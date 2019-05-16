using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Cloud.MegaNz {
    public class MegaNzConfig : DataModel {
        public const string ModelId = "configs/mega_nz";

        static MegaNzConfigServiceClient client;

        public static MegaNzConfigServiceClient Client => client =
            client ?? new MegaNzConfigRestServiceClient();

        public Dictionary<string, AccountInfo> Accounts { get; private set; }
    }

    public class AccountInfo {
        public string Username { get; set; }

        public string Password { get; set; }
    }

    public interface MegaNzConfigServiceClient : PimixServiceClient<MegaNzConfig> {
    }

    public class MegaNzConfigRestServiceClient : PimixServiceRestClient<MegaNzConfig>, MegaNzConfigServiceClient {
    }
}
