using System;
using System.Collections.Generic;

namespace Kifa.Service {
    public class DataMetadata {
        public LinkingMetadata? Linking { get; set; }
        public VirtualLinkingMetadata? VirtualLinking { get; set; }
        public FreshnessMetadata? Freshness { get; set; }
    }

    public class LinkingMetadata {
        // Having this value means its data is in Target, but it's still a concrete instance.
        public string? Target { get; set; }

        // If this one is the source, this field will be populated with all other instances with the data.
        public HashSet<string>? Links { get; set; }
    }

    public class VirtualLinkingMetadata {

        // Having this value means its data is in VirtualTarget and the item should be removed if other targets are
        // removed.
        public string? VirtualTarget { get; set; }

        // If this one is the source, this field will be populated with all other instances that can be automatically
        // generated.
        public HashSet<string>? VirtualLinks { get; set; }
    }

    public class FreshnessMetadata {
        public DateTimeOffset? LastUpdated { get; set; }
        public DateTimeOffset? LastRefreshed { get; set; }
    }
}
