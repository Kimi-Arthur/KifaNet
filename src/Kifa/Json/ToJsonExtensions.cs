using Newtonsoft.Json;

namespace Kifa;

public static class ToJsonExtensions {
    public static string ToPrettyJson<T>(this T data)
        => JsonConvert.SerializeObject(data, KifaJsonSerializerSettings.Pretty);

    public static string ToJson<T>(this T data)
        => JsonConvert.SerializeObject(data, KifaJsonSerializerSettings.Default);
}
