using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Service;

public class DataMetadata {
    public LinkingMetadata? Linking { get; set; }

    // Content version date (when the content was modified or ForceRefreshBefore applied).
    [JsonConverter(typeof(DataMetadataVersionJsonConverter))]
    public DateTimeOffset? Version { get; set; }

    DateTimeOffset? lastRefreshed;

    // When Fill() was last executed against remote sources.
    [JsonConverter(typeof(DataMetadataVersionJsonConverter))]
    public DateTimeOffset? LastRefreshed {
        get => lastRefreshed ?? Version;
        set => lastRefreshed = value;
    }

    public bool ShouldSerializeLastRefreshed() => lastRefreshed != null && lastRefreshed != Version;

    // Overrides that will apply after Fill() is called.
    public Dictionary<string, object> Overrides { get; set; } = new();

    [JsonIgnore]
    [YamlIgnore]
    public bool IsEmpty => Linking == null && Version == null && lastRefreshed == null && (Overrides == null || Overrides.Count == 0);
}

public class LinkingMetadata {
    // Having this value means its data is in Target, but it's still a concrete instance.
    public string? Target { get; set; }

    // If this one is the source, this field will be populated with all other instances with the data.
    public SortedSet<string>? Links { get; set; }

    // If this one is the source, this field will be populated with all other instances with the data.
    public SortedSet<string>? VirtualLinks { get; set; }
}

public class DataMetadataVersionJsonConverter : JsonConverter<DateTimeOffset?> {
    public override void WriteJson(JsonWriter writer, DateTimeOffset? value, JsonSerializer serializer) {
        if (value == null) {
            writer.WriteNull();
        } else {
            writer.WriteValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzz"));
        }
    }

    public override DateTimeOffset? ReadJson(JsonReader reader, Type objectType, DateTimeOffset? existingValue,
        bool hasExistingValue, JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.Null) {
            return null;
        }

        if (reader.TokenType == JsonToken.Integer) {
            // Legacy integer version: treat as null so it is refreshed with date version.
            return null;
        }

        if (reader.TokenType == JsonToken.Date) {
            if (reader.Value is DateTime dt) {
                return new DateTimeOffset(dt);
            }

            if (reader.Value is DateTimeOffset dto) {
                return dto;
            }
        }

        if (reader.TokenType == JsonToken.String) {
            var str = (string?) reader.Value;
            if (str == null || str.Length == 0) {
                return null;
            }

            if (DateTimeOffset.TryParse(str, out var parsed)) {
                return parsed;
            }
        }

        return null;
    }
}

public static class DataFreshnessExtensions {
    public static void ResetRefreshDate(this DataModel data) {
        data.Metadata ??= new DataMetadata();
        data.Metadata.Version = null;
        data.Metadata.LastRefreshed = null;
    }

    public static bool NeedRefresh(this DataModel data) {
        if (data.Metadata?.Version == null) {
            return data.FillByDefault;
        }

        if (data.ForceRefreshBefore != null && data.Metadata.Version < data.ForceRefreshBefore) {
            return true;
        }

        var lastChecked = data.Metadata.LastRefreshed ?? data.Metadata.Version;
        if (data.RefreshInterval != null && lastChecked + data.RefreshInterval < DateTimeOffset.UtcNow) {
            return true;
        }

        return false;
    }
}
