using System.Collections.Generic;

namespace Kifa.Memrise {
    public class MemriseWord {
        public string ThingId { get; set; }

        public Dictionary<string, string> Data { get; set; }

        public List<string> AudioLinks { get; set; }
    }
}
