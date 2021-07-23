using Kifa.Service;

namespace Kifa.Apps.MomentCounter {
    public class Counter : DataModel<Counter> {
        public string Title { get; set; }
        
        // Must have this for starting point.
        public Date FromDate { get; set; }

        // Can be null, meaning going forever. If both dates are set, total can be converted to average.
        public Date ToDate { get; set; }

        public Unit Unit { get; set; }
        
        public double TotalTarget { get; set; }

        public double AverageTarget { get; set; }
    }
}
