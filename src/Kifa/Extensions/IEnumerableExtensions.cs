using System.Collections.Generic;
using System.Linq;

namespace Kifa {
    public static class IEnumerableExtensions {
        public static IEnumerable<T> ExceptNull<T>(this IEnumerable<T?> enumerable) where T : class =>
            enumerable.Where(e => e != null).Select(e => e!);
    }
}