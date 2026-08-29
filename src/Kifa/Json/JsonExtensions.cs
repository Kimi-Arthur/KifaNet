using Newtonsoft.Json;

namespace Kifa;

public static class JsonExtensions {
    public static string ToJson<T>(this T data, JsonSerializerSettings settings)
        => JsonConvert.SerializeObject(data, settings);

    public static string ToJson<T>(this T data)
        => data.ToJson(KifaJsonSerializerSettings.Default);

    public static string ToPrettyJson<T>(this T data)
        => data.ToJson(KifaJsonSerializerSettings.Pretty);

    public static string ToCamelCaseJson<T>(this T data)
        => data.ToJson(KifaJsonSerializerSettings.CamelCase);

    public static string ToDataJson<T>(this T data)
        => data.ToJson(KifaJsonSerializerSettings.DataContent);

    public static T? FromJson<T>(this string? json, JsonSerializerSettings settings)
        => json == null ? default : JsonConvert.DeserializeObject<T>(json, settings);

    public static T? FromJson<T>(this string? json)
        => json.FromJson<T>(KifaJsonSerializerSettings.Default);

    public static T? FromCamelCaseJson<T>(this string? json)
        => json.FromJson<T>(KifaJsonSerializerSettings.CamelCase);
}
