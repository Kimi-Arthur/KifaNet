using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;

namespace Kifa.Service;

/// <summary>
/// When used, specify a public const string field named ModelId.
/// </summary>
public abstract class DataModel {
    public const string VirtualItemPrefix = "/$/";

    [YamlMember(Order = -1)]
    public string Id {
        get => Late.Get(id);
        set => Late.Set(ref id, value);
    }

    string? id;

    [JsonIgnore]
    [YamlIgnore]
    public string? RealId => Metadata?.Linking?.Target ?? Id;

    [JsonProperty("$metadata")]
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
    public bool IsVirtualItem() => Id?.StartsWith(VirtualItemPrefix) ?? false;

    public SortedSet<string> GetAllLinks()
        => Metadata?.Linking?.Links == null
            ? new SortedSet<string> {
                Id
            }
            : new SortedSet<string>(Metadata.Linking.Links.Append(RealId!));

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
        => JsonConvert.SerializeObject(this, Defaults.PrettyJsonSerializerSettings);

    public override int GetHashCode() => ToString().GetHashCode();

    public override bool Equals(object? obj)
        => GetType().IsInstanceOfType(obj) && ToString() == obj?.ToString();
}

public abstract class DataModel<TDataModel> : DataModel where TDataModel : DataModel {
    [JsonProperty("$translations")]
    public Dictionary<string, TDataModel>? Translations { get; set; }

    public TDataModel With(Action<TDataModel> update) {
        update((this as TDataModel)!);
        return (this as TDataModel)!;
    }
}
