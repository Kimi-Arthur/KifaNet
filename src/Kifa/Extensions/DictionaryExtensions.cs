using System.Collections.Generic;

namespace Kifa {
    public static class DictionaryExtensions {
        public static TValue Pop<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) {
            if (dict.TryGetValue(key, out var value)) {
                dict.Remove(key);
                return value;
            }

            return default;
        }
    }
}
