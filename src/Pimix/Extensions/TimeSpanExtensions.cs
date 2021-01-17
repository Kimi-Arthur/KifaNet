using System;

namespace Pimix {
    public static class TimeSpanExtensions {
        public static TimeSpan Or(this TimeSpan timeSpan, TimeSpan orTimeSpan) =>
            timeSpan == TimeSpan.Zero ? orTimeSpan : timeSpan;
    }
}
