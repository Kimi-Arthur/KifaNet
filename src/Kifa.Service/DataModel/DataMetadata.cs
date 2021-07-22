using System;
using System.Collections.Generic;

namespace Kifa.Service {
    public class DataMetadata {
        public string Id { get; set; }

        // If this one is the source, this field will be populated with all other instances with the data.
        public HashSet<string> Links { get; set; }

        // If this one is the source, this field will be populated with all other instances that can be automatically
        // generated.
        public HashSet<string> VirtualLinks { get; set; }

        public DateTimeOffset? LastUpdated { get; set; }
        public DateTimeOffset? LastRefreshed { get; set; }
    }
}
