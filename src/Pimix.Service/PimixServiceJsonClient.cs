using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Pimix.Service {
    public class PimixServiceJsonClient : PimixServiceClient {
        public static string DataFolder { get; set; }

        public override TDataModel Get<TDataModel>(string id) {
            var modelId = GetModelInfo<TDataModel>().modelId;
            return JsonConvert.DeserializeObject<TDataModel>(
                File.ReadAllText($"{DataFolder}/{modelId}/{id.Trim('/')}.json"));
        }

        public override void Set<TDataModel>(TDataModel data, string id = null) {
            var (idProperty, modelId) = GetModelInfo<TDataModel>();
            id = id ?? idProperty.GetValue(data) as string;

            File.WriteAllText($"{DataFolder}/{modelId}/{id.Trim('/')}.json",
                JsonConvert.SerializeObject(data, Defaults.PrettyJsonSerializerSettings));
        }

        public override void Update<TDataModel>(TDataModel data, string id = null) {
            var (idProperty, modelId) = GetModelInfo<TDataModel>();
            id = id ?? idProperty.GetValue(data) as string;
            var original = Get<TDataModel>(id);
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings), original);

            File.WriteAllText($"{DataFolder}/{modelId}/{id.Trim('/')}.json",
                JsonConvert.SerializeObject(original, Defaults.PrettyJsonSerializerSettings));
        }

        public override void Delete<TDataModel>(string id) {
            throw new NotImplementedException();
        }

        public override void Link<TDataModel>(string targetId, string linkId) {
            throw new NotImplementedException();
        }

        public override TResponse Call<TDataModel, TResponse>(string action, string id = null,
            Dictionary<string, object> parameters = null) => throw new NotImplementedException();
    }
}
