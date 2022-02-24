using Newtonsoft.Json;

namespace Kifa.Service;

public static class CloneableExtension {
    public static TDataModel Clone<TDataModel>(this TDataModel data)
        => JsonConvert.DeserializeObject<TDataModel>(
            JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings),
            Defaults.JsonSerializerSettings)!;
}
