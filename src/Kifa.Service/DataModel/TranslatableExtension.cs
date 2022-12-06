using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kifa.Service;

public interface Translatable<T> where T : new() {
    [JsonProperty("$translations")]
    public Dictionary<string, T>? Translations { get; set; }
}

public static class TranslatableExtension {
    public static T GetTranslated<T>(this T data, string language)
        where T : Translatable<T>, new() {
        data = data.Clone();
        var dataInLanguage =
            (data.Translations ?? new Dictionary<string, T>()).GetValueOrDefault(language);
        if (dataInLanguage != null) {
            JsonConvert.PopulateObject(
                JsonConvert.SerializeObject(dataInLanguage, KifaJsonSerializerSettings.Default),
                data, KifaJsonSerializerSettings.Merge);
        }

        data.Translations = null;
        return data;
    }
}
