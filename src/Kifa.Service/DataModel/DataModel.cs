using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Service;

public interface WithModelId<T> where T : DataModel, WithModelId<T> {
    public static abstract string ModelId { get; }

    // This is a useful fallback used in Link.Data.get.
    public static virtual KifaServiceClient<T> Client { get; set; } =
        new KifaServiceRestClient<T>();

    static Dictionary<string, PropertyInfo>? allProperties;

    public static virtual Dictionary<string, PropertyInfo> AllProperties
        => allProperties ??= GatherAllProperties();

    static Dictionary<string, PropertyInfo> GatherAllProperties()
        => typeof(T).GetProperties().Where(prop => prop.GetSetMethod()?.IsStatic == false)
            .ToDictionary(property => property.Name);

    static List<(PropertyInfo property, string Suffix)>? externalProperties;

    public static virtual List<(PropertyInfo property, string Suffix)> ExternalProperties
        => externalProperties ??= GatherExternalProperties().ToList();

    static IEnumerable<(PropertyInfo property, string Suffix)> GatherExternalProperties() {
        foreach (var property in T.AllProperties.Values) {
            var attribute = property.GetCustomAttribute<ExternalPropertyAttribute>();
            if (attribute == null) {
                continue;
            }

            if (property.PropertyType != typeof(string)) {
                throw new InvalidExternalPropertyException(
                    $"Property {property} marked with {nameof(ExternalPropertyAttribute)} should be of type string, but is {property.PropertyType}.");
            }

            if (attribute.Suffix.EndsWith("json")) {
                throw new InvalidExternalPropertyException(
                    $"Property {property} marked with {nameof(ExternalPropertyAttribute)} should not use json as extension, but used {attribute.Suffix}.");
            }

            yield return (property, attribute.Suffix);
        }
    }
}

/// <summary>
/// When used, specify a public const string field named ModelId.
/// </summary>
public abstract class DataModel : IEquatable<DataModel> {
    public const string VirtualItemPrefix = "/$/";

    [YamlMember(Order = -1)]
    [JsonProperty(Order = -2)]
    public virtual string? Id { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    public string? RealId => Metadata?.Linking?.Target ?? Id;

    [JsonProperty("$metadata", Order = -3)]
    [YamlIgnore]
    public DataMetadata? Metadata { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    public virtual bool FillByDefault => false;

    [JsonIgnore]
    [YamlIgnore]
    public virtual int CurrentVersion => 0;

    // Return value is the next refresh time.
    // Or null if no refresh is planned.
    // If this is not implemented,
    // FillByDefault and CurrentVersion should not be implemented, either.
    public virtual DateTimeOffset? Fill() => throw new NoNeedToFillException();

    public virtual SortedSet<string> GetVirtualItems() => new();
    public bool IsVirtualItem() => Id.StartsWith(VirtualItemPrefix);

    public SortedSet<string> GetAllLinks()
        => Metadata?.Linking?.Links == null ? [RealId] : [..Metadata.Linking.Links, RealId];

    public string ToDataJson() => this.ToJson(KifaJsonSerializerSettings.DataContent);

    public virtual bool Equals(DataModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        if (GetType() != other.GetType()) {
            return false;
        }

        return ToDataJson() == other.ToDataJson();
    }

    public override bool Equals(object? obj) => Equals(obj as DataModel);

    public override int GetHashCode() => ToDataJson().GetHashCode();

    public override string ToString() => this.ToPrettyJson();
}
