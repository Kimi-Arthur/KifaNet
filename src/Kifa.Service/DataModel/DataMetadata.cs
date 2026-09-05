using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Service;

public class DataMetadata {
    public LinkingMetadata? Linking { get; set; }

    // Content version date (when the content was modified or ForceRefreshBefore applied).
    public DataVersion? Version { get; set; }

    DataVersion? lastRefreshed;

    // When Fill() was last executed against remote sources.
    public DataVersion? LastRefreshed {
        get => lastRefreshed ?? Version;
        set => lastRefreshed = value;
    }

    public bool ShouldSerializeLastRefreshed() => lastRefreshed != null && lastRefreshed != Version;

    // Overrides that will apply after Fill() is called.
    public Dictionary<string, object> Overrides { get; set; } = new();

    [JsonIgnore]
    [YamlIgnore]
    public bool IsEmpty
        => Linking == null && Version == null && lastRefreshed == null &&
           (Overrides == null || Overrides.Count == 0);
}

public class LinkingMetadata {
    // Having this value means its data is in Target, but it's still a concrete instance.
    public string? Target { get; set; }

    // If this one is the source, this field will be populated with all other instances with the data.
    public SortedSet<string>? Links { get; set; }

    // If this one is the source, this field will be populated with all other instances with the data.
    public SortedSet<string>? VirtualLinks { get; set; }
}

public static class DataFreshnessExtensions {
    public static void ResetRefreshDate(this DataModel data) {
        data.Metadata ??= new DataMetadata();
        data.Metadata.Version = null;
        data.Metadata.LastRefreshed = null;
    }

    public static bool NeedRefresh(this DataModel data) {
        if (data.Metadata?.Version == null) {
            return true;
        }

        if (data.ForceRefreshBefore != null && data.Metadata.Version < data.ForceRefreshBefore) {
            return true;
        }

        var lastChecked = data.Metadata.LastRefreshed ?? data.Metadata.Version;
        if (data.RefreshInterval != null) {
            return (lastChecked + data.RefreshInterval)?.Value < DateTimeOffset.UtcNow;
        }

        return true;
    }
}
