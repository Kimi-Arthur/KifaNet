using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Cloud.Swisscom {
    public class SwisscomConfig : DataModel<SwisscomConfig> {
        public const string ModelId = "configs/swisscom";

        static KifaServiceClient<SwisscomConfig> client;

        public static KifaServiceClient<SwisscomConfig> Client => client ??= new SwisscomConfigRestServiceClient();

        public Dictionary<string, Dictionary<string, long>> Reservations { get; set; }

        public List<StorageMapping> StorageMappings { get; set; }
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
