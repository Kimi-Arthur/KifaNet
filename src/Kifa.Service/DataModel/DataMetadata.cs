using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Service;

public class DataMetadata {
    public LinkingMetadata? Linking { get; set; }
    public FreshnessMetadata? Freshness { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    public bool IsEmpty => Linking == null && Freshness == null;
}

public class LinkingMetadata {
    // Having this value means its data is in Target, but it's still a concrete instance.
    public string? Target { get; set; }

    // If this one is the source, this field will be populated with all other instances with the data.
    public SortedSet<string>? Links { get; set; }

    // If this one is the source, this field will be populated with all other instances with the data.
    public SortedSet<string>? VirtualLinks { get; set; }
}

public class FreshnessMetadata {
    public DateTimeOffset? NextRefresh { get; set; }
}

public static class FreshnessMetadataExtensions {
    public static void ResetRefreshDate(this DataModel data) {
        data.Metadata ??= new DataMetadata();
        data.Metadata.Freshness = new FreshnessMetadata {
            NextRefresh = Date.Zero
        };
    }

    public static bool NeedRefresh(this DataModel data)
        => data.FillByDefault && data.Metadata?.Freshness?.NextRefresh == null ||
           data.Metadata?.Freshness?.NextRefresh + TimeSpan.FromSeconds(5) < DateTimeOffset.UtcNow;
}
