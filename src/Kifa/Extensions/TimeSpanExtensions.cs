using System;

namespace Kifa;

public static class TimeSpanExtensions {
    public static TimeSpan Or(this TimeSpan timeSpan, TimeSpan orTimeSpan)
        => timeSpan == TimeSpan.Zero ? orTimeSpan : timeSpan;
}
