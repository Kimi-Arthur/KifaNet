using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Kifa;

public static class Defaults {
    public static readonly JsonSerializerSettings JsonSerializerSettings = new() {
        EqualityComparer = ReferenceEqualityComparer.Instance,
        DateFormatString = "yyyy-MM-dd HH:mm:ss.ffffff",
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        ContractResolver = new OrderedContractResolver {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        Converters = new List<JsonConverter> {
            new StringEnumConverter(new SnakeCaseNamingStrategy()),
            new GenericJsonConverter()
        },
        MissingMemberHandling = MissingMemberHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };

    public static readonly JsonSerializerSettings PrettyJsonSerializerSettings = new() {
        EqualityComparer = ReferenceEqualityComparer.Instance,
        DateFormatString = "yyyy-MM-dd HH:mm:ss.ffffff",
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        Formatting = Formatting.Indented,
        ContractResolver = new OrderedContractResolver {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        Converters = new List<JsonConverter> {
            new StringEnumConverter(new SnakeCaseNamingStrategy()),
            new GenericJsonConverter()
        },
        MissingMemberHandling = MissingMemberHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };
}
