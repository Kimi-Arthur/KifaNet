using Newtonsoft.Json;

namespace Kifa;

public static class CloneableExtension {
    public static T Clone<T>(this T data)
        => JsonConvert.DeserializeObject<T>(
            JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings),
            Defaults.JsonSerializerSettings)!;
}
