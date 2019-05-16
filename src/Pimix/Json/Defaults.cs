using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Pimix {
    public static class Defaults {
        public static readonly JsonSerializerSettings JsonSerializerSettings =
            new JsonSerializerSettings {
                ContractResolver = new DefaultContractResolver {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                },
                Converters = new List<JsonConverter> {
                    new StringEnumConverter(),
                    new GenericJsonConverter()
                },
                NullValueHandling = NullValueHandling.Ignore,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
            };

        public static readonly JsonSerializerSettings PrettyJsonSerializerSettings =
            new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                },
                Converters = new List<JsonConverter> {
                    new StringEnumConverter(),
                    new GenericJsonConverter()
                },
                NullValueHandling = NullValueHandling.Ignore,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
            };
    }
}
