using Newtonsoft.Json;

namespace Kifa {
    public static class ToJsonExtensions {
        public static string ToPrettyJson<T>(this T data) =>
            JsonConvert.SerializeObject(data, Defaults.PrettyJsonSerializerSettings);
    }
}
