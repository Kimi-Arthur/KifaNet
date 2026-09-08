using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kifa;

public class OrderedContractResolver : DefaultContractResolver {
    public HashSet<string>? IgnoredProperties { get; init; }

    public bool IgnoreExternalProperties { get; init; } = false;

    protected override IList<JsonProperty> CreateProperties(Type type,
        MemberSerialization memberSerialization) {
        return base.CreateProperties(type, memberSerialization).ToList();
    }

    static readonly NullabilityInfoContext NullabilityContext = new();

    protected override JsonProperty CreateProperty(MemberInfo member,
        MemberSerialization memberSerialization) {
        var property = base.CreateProperty(member, memberSerialization);

        if (IgnoreExternalProperties &&
            member.CustomAttributes.Any(a => a.AttributeType.Name == "ExternalPropertyAttribute")) {
            property.ShouldSerialize = _ => false;
        }

        if (IgnoredProperties != null &&
            (IgnoredProperties.Contains(member.Name) ||
             (property.PropertyName != null && IgnoredProperties.Contains(property.PropertyName)))) {
            property.Ignored = true;
            return property;
        }

        if (IsNullable(member)) {
            // Don't do anything special for nullable reference types.
            return property;
        }

        if (property.PropertyType == typeof(string)) {
            property.DefaultValue = "";
        }

        if (property.PropertyType?.IsEnum ?? false) {
            property.DefaultValue = 0;
        }

        if (property.ShouldSerialize == null && property.PropertyType != null &&
            IsCollectionType(property.PropertyType)) {
            property.ShouldSerialize =
                instance => HasElements(property.ValueProvider?.GetValue(instance));
            property.DefaultValueHandling = DefaultValueHandling.Ignore;
        }

        return property;
    }

    static bool IsCollectionType(Type type)
        => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);

    static bool HasElements(object? value) {
        if (value == null) {
            return false;
        }

        if (value is ICollection col) {
            return col.Count > 0;
        }

        if (value is IDictionary dict) {
            return dict.Count > 0;
        }

        if (value is IEnumerable enumerable) {
            var enumerator = enumerable.GetEnumerator();
            using var disposable = enumerator as IDisposable;
            return enumerator.MoveNext();
        }

        return true;
    }

    static bool IsNullable(MemberInfo member) {
        // The lock is needed as `NullabilityContext.Create` is not thread safe.
        lock (NullabilityContext) {
            // Reference: https://devblogs.microsoft.com/dotnet/announcing-net-6-preview-7/#getting-top-level-nullability-information
            return member is PropertyInfo info &&
                   NullabilityContext.Create(info).WriteState is NullabilityState.Nullable;
        }
    }
}
