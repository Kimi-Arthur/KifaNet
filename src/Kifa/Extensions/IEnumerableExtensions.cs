using System;
using System.Collections.Generic;
using System.Linq;

namespace Kifa;

public static class IEnumerableExtensions {
    public static IEnumerable<T> ExceptNull<T>(this IEnumerable<T?> enumerable) where T : class
        => enumerable.Where(e => e != null).Select(e => e!);

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) {
        foreach (var item in source) {
            action(item);
        }
    }

    public static TSource? MaxBy<TSource>(this IEnumerable<TSource> source,
        Func<TSource, long> keySelector) where TSource : struct
        => source.Select(s => (TSource?) s)
            .MaxBy(s => s == null ? long.MinValue : keySelector(s.Value));

    public static TSource? MinBy<TSource>(this IEnumerable<TSource> source,
        Func<TSource, long> keySelector) where TSource : struct
        => source.Select(s => (TSource?) s)
            .MinBy(s => s == null ? long.MaxValue : keySelector(s.Value));
}
