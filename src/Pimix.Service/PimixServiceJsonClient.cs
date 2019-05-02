using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Pimix.Service {
    public class PimixServiceJsonClient {
        public static string DataFolder { get; set; }
    }

    public class PimixServiceJsonClient<TDataModel> : PimixServiceClient<TDataModel> {
        public override TDataModel Get(string id) {
            return JsonConvert.DeserializeObject<TDataModel>(
                File.ReadAllText($"{PimixServiceJsonClient.DataFolder}/{modelId}/{id.Trim('/')}.json"),
                Defaults.JsonSerializerSettings);
        }

        public override void Set(TDataModel data, string id = null) {
            id = id ?? idProperty.GetValue(data) as string;

            File.WriteAllText($"{PimixServiceJsonClient.DataFolder}/{modelId}/{id.Trim('/')}.json",
                JsonConvert.SerializeObject(data, Defaults.PrettyJsonSerializerSettings));
        }

        public override void Update(TDataModel data, string id = null) {
            id = id ?? idProperty.GetValue(data) as string;
            var original = Get(id);
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings), original);

            File.WriteAllText($"{PimixServiceJsonClient.DataFolder}/{modelId}/{id.Trim('/')}.json",
                JsonConvert.SerializeObject(original, Defaults.PrettyJsonSerializerSettings));
        }

        public override void Delete(string id) {
            throw new NotImplementedException();
        }

        public override void Link(string targetId, string linkId) {
            throw new NotImplementedException();
        }

        public override TResponse Call<TResponse>(string action, string id = null,
            Dictionary<string, object> parameters = null) => throw new NotImplementedException();
    }
}
