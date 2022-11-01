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

    protected override JsonProperty CreateProperty(MemberInfo member,
        MemberSerialization memberSerialization) {
        var property = base.CreateProperty(member, memberSerialization);

        if (property.PropertyType == typeof(string)) {
            property.DefaultValue = "";
        }

        if (property.PropertyType?.IsGenericType ?? false) {
            if (property.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) {
                property.DefaultValue = Activator.CreateInstance(property.PropertyType);
                property.ShouldSerialize = instance
                    => (property.ValueProvider.GetValue(instance) as IList) is {
                        Count: > 0
                    };
            }

            if (property.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                property.DefaultValue = Activator.CreateInstance(property.PropertyType);
                property.ShouldSerialize = instance
                    => (property.ValueProvider.GetValue(instance) as IDictionary) is {
                        Count: > 0
                    };
            }
        }

        return property;
    }
}
