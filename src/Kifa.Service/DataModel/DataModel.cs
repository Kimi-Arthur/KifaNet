using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
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
public abstract class DataModel {
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

    public SortedSet<string> GetOtherLinks() => Metadata?.Linking?.Links ?? [];

    public SortedSet<string> GetAllLinks()
        => Metadata?.Linking?.Links == null ? [RealId] : [..Metadata.Linking.Links, RealId];

    // Not finished
    public string Compare<TDataModel>(TDataModel other) {
        if (!(this is TDataModel model)) {
            return "<Different type>";
        }

        var myJson = JToken.Parse(ToString());
        var otherJson = JToken.Parse(ToString());
        var diffToken = CompareJToken(myJson, otherJson);
        return diffToken.ToString();
    }

    JToken CompareJToken(JToken myJson, JToken otherJson) {
        var result = new JArray();
        if (myJson.Type != otherJson.Type) {
            var myToken = new JObject();
            myToken["-"] = myJson;
            result.Add(myToken);
            var otherToken = new JObject();
            otherToken["+"] = otherJson;
            result.Add(otherToken);
            return result;
        }

        if (myJson.Type == JTokenType.Array) {
            foreach (var childPair in myJson.Children().Zip(otherJson.Children())) {
                if (childPair.First != childPair.Second) {
                    if (childPair.First != null) {
                        var myToken = new JObject();
                        myToken["-"] = myJson;
                        result.Add(myToken);
                    }
                }
            }
        }

        return result;
    }

    public override string ToString()
        => JsonConvert.SerializeObject(this, KifaJsonSerializerSettings.Pretty);

    public override int GetHashCode() => ToString().GetHashCode();

    public override bool Equals(object? obj)
        => GetType().IsInstanceOfType(obj) && ToString() == obj?.ToString();
}
