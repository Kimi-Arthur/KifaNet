using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kifa;

public class OrderedContractResolver : DefaultContractResolver {
    protected override IList<JsonProperty> CreateProperties(Type type,
        MemberSerialization memberSerialization) {
        return base.CreateProperties(type, memberSerialization).OrderBy(p => p.PropertyName)
            .ToList();
    }

    static readonly NullabilityInfoContext NullabilityContext = new();

    protected override JsonProperty CreateProperty(MemberInfo member,
        MemberSerialization memberSerialization) {
        var property = base.CreateProperty(member, memberSerialization);

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

        if (property.PropertyType?.IsGenericType ?? false) {
            if (property.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) {
                property.ShouldSerialize = instance
                    => (property.ValueProvider.GetValue(instance) as IList) is {
                        Count: > 0
                    };
            }

            if (property.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                property.ShouldSerialize = instance
                    => (property.ValueProvider.GetValue(instance) as IDictionary) is {
                        Count: > 0
                    };
            }
        }

        return property;
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
