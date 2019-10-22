using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Cloud.MegaNz {
    public class MegaNzConfig : DataModel {
        public const string ModelId = "configs/mega_nz";

        static PimixServiceClient<MegaNzConfig> client;

        public static PimixServiceClient<MegaNzConfig> Client => client ??= new PimixServiceRestClient<MegaNzConfig>();

        public Dictionary<string, AccountInfo> Accounts { get; set; }
    }

    public class AccountInfo {
        public string Username { get; set; }

        public string Password { get; set; }
    }
}
