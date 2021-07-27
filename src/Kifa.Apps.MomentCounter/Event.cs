using System;
using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Apps.MomentCounter {
    public class Event : DataModel<Event> {
        public DateTime DateTime { get; set; }

        // Should match Counter's units.
        public List<int> Values { get; set; }
    }
}
