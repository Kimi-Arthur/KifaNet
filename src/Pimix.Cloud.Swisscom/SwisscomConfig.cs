using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Cloud.Swisscom {
    public class SwisscomConfig : DataModel {
        public const string ModelId = "configs/swisscom";

        static KifaServiceClient<SwisscomConfig> client;

        public static KifaServiceClient<SwisscomConfig> Client => client ??= new SwisscomConfigRestServiceClient();

        public Dictionary<string, SwisscomAccount> Accounts { get; set; }

        public Dictionary<string, Dictionary<string, long>> Reservations { get; set; }

        public List<StorageMapping> StorageMappings { get; set; }
    }

    public partial class SwisscomAccount {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public long TotalQuota { get; set; }
        public long UsedQuota { get; set; }

        public long ReservedQuota { get; set; }
    }

    public class StorageMapping {
        public string Pattern { get; set; }
        public List<string> Accounts { get; set; }
    }

    public interface SwisscomConfigServiceClient : KifaServiceClient<SwisscomConfig> {
    }

    public class SwisscomConfigRestServiceClient : KifaServiceRestClient<SwisscomConfig>, SwisscomConfigServiceClient {
    }
}
