using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Pimix {
    public static class Defaults {                
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings {
            ContractResolver = new DefaultContractResolver {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore
        };
    }
}