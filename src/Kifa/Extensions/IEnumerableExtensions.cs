using System;
using System.Collections.Generic;
using System.Linq;

namespace Kifa;

public static class IEnumerableExtensions {
    public static IEnumerable<T> ExceptNull<T>(this IEnumerable<T?> enumerable) where T : class
        => enumerable.Where(e => e != null).Select(e => e!);

    public static IEnumerable<T> OnlyNonNull<T>(this IEnumerable<T?> enumerable) where T : class
        => enumerable.Select(e => e.Checked());

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) {
        foreach (var item in source) {
            action(item);
        }
    }

    public static IEnumerable<T> Dedup<T>(this IEnumerable<T> source) {
        return source.Dedup(value => value);
    }

    public static IEnumerable<T> Dedup<T, R>(this IEnumerable<T> source, Func<T, R> transform) {
        // TODO: Fix the analysis errors here.
        var first = true;
        R lastConverted = default;
        foreach (var value in source) {
            var converted = transform(value);
            if (first || !converted.Equals(lastConverted)) {
                yield return value;
            }

            first = false;
            lastConverted = converted;
        }
    }
}
