using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Apps.MomentCounter {
    public class User : DataModel<User> {
        public string Name { get; set; }
        public Settings Settings { get; set; }

        public List<Link<Counter>> Counters { get; set; }
    }
}
