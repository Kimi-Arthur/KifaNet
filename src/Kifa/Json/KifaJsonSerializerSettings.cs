using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Kifa;

public static class KifaJsonSerializerSettings {
    static JsonSerializerSettings GetSettings(bool indented = false, bool merge = false,
        bool camelCase = false, bool ignoreMetadata = false, bool ignoreExternalProperties = false,
        bool ignoreExpandedLinks = false)
        => new() {
            EqualityComparer = ReferenceEqualityComparer.Instance,
            DateFormatString = "yyyy-MM-dd HH:mm:ss.ffffff",
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ContractResolver = new OrderedContractResolver {
                NamingStrategy =
                    camelCase ? new CamelCaseNamingStrategy() : new SnakeCaseNamingStrategy(),
                IgnoredProperties = ignoreMetadata ? ["Metadata", "$metadata"] : null,
                IgnoreExternalProperties = ignoreExternalProperties
            },
            Converters = new List<JsonConverter> {
                new StringEnumConverter(new SnakeCaseNamingStrategy()),
                new GenericJsonConverter(ignoreExpandedLinks: ignoreExpandedLinks),
                new DateTimeOffsetConverter()
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

    // Content comparisons and on-disk .json files must always serialize Link<T> as canonical ID strings
    // even if Expand() was called on the in-memory instance.
    public static readonly JsonSerializerSettings DataContent =
        GetSettings(ignoreMetadata: true, ignoreExpandedLinks: true);

    public static readonly JsonSerializerSettings Disk =
        GetSettings(indented: true, ignoreExternalProperties: true, ignoreExpandedLinks: true);
}
