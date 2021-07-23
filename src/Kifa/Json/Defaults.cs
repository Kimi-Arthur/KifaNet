using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Kifa {
    public static class Defaults {
        public static readonly JsonSerializerSettings JsonSerializerSettings = new() {
            DateFormatString = "yyyy-MM-dd HH:mm:ss.ffffffzzz",
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ContractResolver = new OrderedContractResolver {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            Converters = new List<JsonConverter> {
                new StringEnumConverter(new SnakeCaseNamingStrategy()),
                new GenericJsonConverter()
            },
            Error = (sender, ev) => ev.ErrorContext.Handled = true,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore
        };

        public static readonly JsonSerializerSettings PrettyJsonSerializerSettings = new() {
            DateFormatString = "yyyy-MM-dd HH:mm:ss.ffffffzzz",
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Formatting = Formatting.Indented,
            ContractResolver = new OrderedContractResolver {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            Converters = new List<JsonConverter> {
                new StringEnumConverter(new SnakeCaseNamingStrategy()),
                new GenericJsonConverter()
            },
            Error = (sender, ev) => ev.ErrorContext.Handled = true,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore
        };
    }

    public class OrderedContractResolver : DefaultContractResolver {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) {
            return base.CreateProperties(type, memberSerialization).OrderBy(p => p.PropertyName).ToList();
        }
    }
}
