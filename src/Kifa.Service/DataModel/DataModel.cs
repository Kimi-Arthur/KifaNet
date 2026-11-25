using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kifa.AI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;

namespace Kifa.Service;

public interface WithModelId<T> where T : DataModel, WithModelId<T> {
    public static abstract string ModelId { get; }

    // Default translation language of the base model file (e.g., Language.English for GermanWord).
    public static virtual Language? DefaultLanguage => null;

    // When true, VirtualLinks may use first-class (non-/$/-prefixed) IDs owned by the canonical
    // target, and manual Link() is disabled for this model.
    public static virtual bool HasFirstClassVirtualLinks => false;

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

// When used, specify a public const string field named ModelId.
public abstract class DataModel : IEquatable<DataModel> {
    public const string VirtualLinkPrefix = "/$/";

    [YamlMember(Order = -1)]
    [JsonProperty(Order = -2)]
    public virtual string? Id { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    public string? RealId => Metadata?.Linking?.Target ?? Id;

    [JsonProperty("$metadata", Order = -3)]
    [YamlIgnore]
    public DataMetadata? Metadata { get; set; }

    public bool ShouldSerializeMetadata() => Metadata?.ShouldSerialize() == true;

    // Controls upstream filling and periodic re-filling:
    // - RefreshIntervals.None (null): model is not fillable (never runs Fill).
    // - RefreshIntervals.Infinity (TimeSpan.MaxValue): fill once on initial creation, then never auto-refresh unless Refresh=true or ForceRefreshBefore.
    // - RefreshIntervals.Zero (TimeSpan.Zero): always attempt to refresh on every Get.
    // - > TimeSpan.Zero: refresh once (LastRefreshed ?? Version) + RefreshInterval <= UtcNow.
    [JsonIgnore]
    [YamlIgnore]
    public abstract TimeSpan? RefreshInterval { get; }

    // Forces items with a Version earlier than this timestamp to be re-filled.
    // NOTE: This timestamp should be set at deployment/release time rather than during development
    // to avoid missing items updated in production prior to deployment.
    [JsonIgnore]
    [YamlIgnore]
    public virtual DataVersion? ForceRefreshBefore => null;

    public virtual DataVersion? GetNextRefresh() =>
        Metadata?.Version != null && RefreshInterval != null && RefreshInterval != RefreshIntervals.Infinity
            ? (Metadata.LastRefreshed ?? Metadata.Version) + RefreshInterval
            : null;

    public virtual bool NeedRefreshFrom(DataModel? upstream)
        => NeedRefreshFrom(upstream?.Metadata?.Version);

    public virtual bool NeedRefreshFrom(DataVersion? upstreamVersion) {
        if (Metadata?.Version == null) {
            return true;
        }

        var lastChecked = Metadata.LastRefreshed ?? Metadata.Version;
        if (ForceRefreshBefore != null && lastChecked < ForceRefreshBefore) {
            return true;
        }

        if (upstreamVersion == null) {
            return true;
        }

        return upstreamVersion > Metadata.Version;
    }

    // Fills the model from upstream. Models use whatever the context offers and ignore the rest.
    public virtual void Fill(FillContext context) => throw new NoNeedToFillException();

    public void Fill() => Fill(new FillContext());

    public virtual SortedSet<string> GetVirtualLinks() => new();
    public bool IsVirtualLink() => Metadata?.Linking?.VirtualLinks?.Contains(Id) == true;

    public void ClearFillErrors() {
        if (Metadata?.FillErrors != null) {
            Metadata.FillErrors = null;
        }
    }

    public void AddFillError(FillError error) {
        if (string.IsNullOrWhiteSpace(error.Type)) {
            return;
        }

        Metadata ??= new DataMetadata();
        Metadata.FillErrors ??= [];
        Metadata.FillErrors.Add(error);
    }

    public void AddFillError(string type, string? target = null, string? location = null,
        string? context = null, string? detail = null)
        => AddFillError(new FillError {
            Type = type,
            Target = target,
            Location = location,
            Context = context,
            Detail = detail
        });

    public FillDecision? FindFillDecision(string key, Dictionary<string, object> inputs) {
        if (Metadata?.Decisions == null || !Metadata.Decisions.TryGetValue(key, out var decision)) {
            return null;
        }

        return JToken.DeepEquals(JToken.FromObject(decision.Inputs), JToken.FromObject(inputs))
            ? decision
            : null;
    }

    public void SetFillDecision(string key, FillDecision decision) {
        Metadata ??= new DataMetadata();
        Metadata.Decisions ??= new SortedDictionary<string, FillDecision>(StringComparer.Ordinal);
        Metadata.Decisions[key] = decision;
    }

    public string? ResolveDecision(FillDecisionQuestion question, LlmClient? llm = null) {
        var inputs = question.BuildInputs();
        var cached = FindFillDecision(question.Key, inputs);
        if (cached != null && question.Choices.Contains(cached.Value, StringComparer.Ordinal)) {
            return cached.Value;
        }

        if (llm == null) {
            return null;
        }

        var result = llm.Choose(question.BuildPrompt(), question.Choices);
        if (result == null || !question.Choices.Contains(result.Value, StringComparer.Ordinal)) {
            return null;
        }

        SetFillDecision(question.Key, new FillDecision {
            Inputs = inputs,
            Value = result.Value,
            Llm = result.Info
        });
        return result.Value;
    }

    public SortedSet<string> GetAllLinks()
        => Metadata?.Linking?.Links == null ? [RealId] : [..Metadata.Linking.Links, RealId];

    public override string ToString() => this.ToPrettyJson();

    public virtual bool IsEmpty() {
        var empty = Activator.CreateInstance(GetType()) as DataModel;
        if (empty != null) {
            empty.Id = Id;
        }

        return Equals(empty);
    }

    public bool Equals(DataModel? other) {
        if (other == null || other.GetType() != GetType()) {
            return false;
        }

        return ReferenceEquals(this, other) || this.ToDataJson() == other.ToDataJson();
    }

    public override bool Equals(object? obj) => Equals(obj as DataModel);

    public override int GetHashCode() => this.ToDataJson().GetHashCode();
}
