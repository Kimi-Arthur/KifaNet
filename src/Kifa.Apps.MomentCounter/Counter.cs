using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Apps.MomentCounter {
    public class Counter : DataModel<Counter> {
        public string Title { get; set; }

        // Must have this for starting point.
        public Date FromDate { get; set; }

        // Can be null, meaning going forever. If both dates are set, total can be converted to average.
        public Date ToDate { get; set; }

        // For now, this should not be changed. Can be added maybe.
        public List<Unit> Units { get; set; }

        public int TotalTarget { get; set; }

        public int AverageTarget { get; set; }

        public List<Link<Event>> Events { get; set; }
    }
}
