using Newtonsoft.Json;

namespace Pimix.Service {
    public abstract class DataModel {
        public string Id { get; set; }
    }

    public static class ClonableExtension {
        public static TDataModel Clone<TDataModel>(this TDataModel data) =>
            JsonConvert.DeserializeObject<TDataModel>(JsonConvert.SerializeObject(data));
    }
}
