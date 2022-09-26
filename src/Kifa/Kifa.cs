using System.Collections.Generic;

namespace Kifa;

public static class Kifa {
    public static T Max<T>(T first, T second)
        => Comparer<T>.Default.Compare(first, second) > 0 ? first : second;

    public static T Min<T>(T first, T second)
        => Comparer<T>.Default.Compare(first, second) < 0 ? first : second;
}
