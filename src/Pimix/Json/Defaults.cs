using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Pimix {
    public static class Defaults {
        public static readonly JsonSerializerSettings JsonSerializerSettings =
            new JsonSerializerSettings {
                ContractResolver = new OrderedContractResolver {
                    NamingStrategy = new SnakeCaseNamingStrategy(true, false)
                },
                Converters = new List<JsonConverter> {
                    new StringEnumConverter(new SnakeCaseNamingStrategy()),
                    new GenericJsonConverter()
                },
                NullValueHandling = NullValueHandling.Ignore,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
            };

        public static readonly JsonSerializerSettings PrettyJsonSerializerSettings =
            new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                ContractResolver = new OrderedContractResolver {
                    NamingStrategy = new SnakeCaseNamingStrategy(true, false)
                },
                Converters = new List<JsonConverter> {
                    new StringEnumConverter(new SnakeCaseNamingStrategy()),
                    new GenericJsonConverter()
                },
                NullValueHandling = NullValueHandling.Ignore,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
            };
    }

    public class OrderedContractResolver : DefaultContractResolver {
        protected override IList<JsonProperty> CreateProperties(Type type,
            MemberSerialization memberSerialization) {
            return base.CreateProperties(type, memberSerialization).OrderBy(p => p.PropertyName).ToList();
        }
    }
}
