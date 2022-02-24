using Newtonsoft.Json;

namespace Kifa.Service;

public static class MergeableExtension {
    public static TDataModel Merge<TDataModel>(this TDataModel data, TDataModel update) {
        var obj = data.Clone();
        JsonConvert.PopulateObject(
            JsonConvert.SerializeObject(update, Defaults.JsonSerializerSettings), obj!,
            Defaults.JsonSerializerSettings);
        return obj;
    }
}
