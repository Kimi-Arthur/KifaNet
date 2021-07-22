using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Apps.MomentCounter {
    public class User : DataModel {
        public string Name { get; set; }
        public Settings Settings { get; set; }
        public List<Counter> Counters { get; set; }
    }
}
