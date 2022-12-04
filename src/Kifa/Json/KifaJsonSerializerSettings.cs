using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Kifa;

public static class KifaJsonSerializerSettings {
    static JsonSerializerSettings GetSettings(bool indented = false, bool merge = false,
        bool camelCase = false)
        => new() {
            EqualityComparer = ReferenceEqualityComparer.Instance,
            DateFormatString = "yyyy-MM-dd HH:mm:ss.ffffff",
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ContractResolver = new OrderedContractResolver {
                NamingStrategy =
                    camelCase ? new CamelCaseNamingStrategy() : new SnakeCaseNamingStrategy()
            },
            Converters = new List<JsonConverter> {
                new StringEnumConverter(new SnakeCaseNamingStrategy()),
                new GenericJsonConverter()
            },
            MissingMemberHandling = MissingMemberHandling.Ignore,
            DefaultValueHandling =
                merge ? DefaultValueHandling.Ignore : DefaultValueHandling.IgnoreAndPopulate,
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Formatting = indented ? Formatting.Indented : Formatting.None
        };

    public static readonly JsonSerializerSettings Default = GetSettings();

    public static readonly JsonSerializerSettings CamelCase = GetSettings(camelCase: true);

    public static readonly JsonSerializerSettings Pretty = GetSettings(indented: true);

    public static readonly JsonSerializerSettings Merge = GetSettings(merge: true);
}
