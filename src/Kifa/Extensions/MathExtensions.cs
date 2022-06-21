using System;

namespace Kifa;

public static class MathExtensions {
    public static long RoundUp(this long value, long period)
        => value % period == 0 ? value : value - value % period + period;

    public static int RoundUp(this int value, int period)
        => value % period == 0 ? value : value - value % period + period;

    public static int RoundUp(this double value, int period = 1)
        => (int) Math.Ceiling(value / period) * period;

    public static long RoundDown(this long value, long period)
        => value % period == 0 ? value : value - value % period;

    public static int RoundDown(this int value, int period)
        => value % period == 0 ? value : value - value % period;

    public static int RoundDown(this double value, int period = 1)
        => (int) Math.Floor(value / period) * period;

    public static int Round(this double value, int period = 1)
        => (int) Math.Round(value / period) * period;
}
