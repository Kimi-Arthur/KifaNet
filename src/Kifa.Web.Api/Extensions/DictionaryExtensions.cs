using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Kifa.Web.Api.Extensions;

public static class DictionaryExtensions {
    public static StringValues GetValueOrDefault(this IHeaderDictionary dictionary, string key,
        StringValues defaultValue)
        => dictionary.TryGetValue(key, out var value) ? value : defaultValue;
}
