using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Service;

public class DataMetadata {
    public LinkingMetadata? Linking { get; set; }

    // If the data version is different from current code, a refresh is needed.
    public int Version { get; set; }

    public FreshnessMetadata? Freshness { get; set; }

    // Overrides that will apply after Fill() is called.
    public Dictionary<string, object> Overrides { get; set; } = new();

    [JsonIgnore]
    [YamlIgnore]
    public bool IsEmpty => Linking == null && Freshness == null && Version == 0;
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

    // Cases when refresh is needed:
    //   1. CurrentVersion < 0 = in development phase
    //   2. data version is older than code version
    //   3. FillByDefault and no refresh date set
    //   4. Now is beyond the set refresh date
    public static bool NeedRefresh(this DataModel data)
        => data.CurrentVersion < 0 || data.CurrentVersion > (data.Metadata?.Version ?? 0) ||
           data.FillByDefault && data.Metadata?.Freshness?.NextRefresh == null ||
           data.Metadata?.Freshness?.NextRefresh < DateTimeOffset.UtcNow;
}
