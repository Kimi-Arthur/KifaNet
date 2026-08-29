using Newtonsoft.Json;

namespace Kifa.Service;

public static class MergeableExtension {
    public static TDataModel Merge<TDataModel>(this TDataModel data, TDataModel update)
        where TDataModel : class {
        var obj = data.Clone();
        JsonConvert.PopulateObject(update.ToJson(), obj!, KifaJsonSerializerSettings.Merge);
        return obj;
    }
}
