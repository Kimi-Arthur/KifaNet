using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Pimix.Service {
    public class PimixServiceJsonClient {
        public static string DataFolder { get; set; }
    }

    public class PimixServiceJsonClient<TDataModel> : PimixServiceClient<TDataModel> {
        Dictionary<string, List<string>> Groups { get; set; } = new Dictionary<string, List<string>>();

        public override TDataModel Get(string id) {
            LoadGroups();

            if (Groups.ContainsKey(id)) {
                var obj = JsonConvert.DeserializeObject<TDataModel>(Read(Groups[id].First().Trim('/')),
                    Defaults.JsonSerializerSettings);
                JsonConvert.PopulateObject(Read(id), obj);
                return obj;
            }

            return JsonConvert.DeserializeObject<TDataModel>(Read(id), Defaults.JsonSerializerSettings);
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

        string Read(string id) =>
            File.ReadAllText($"{PimixServiceJsonClient.DataFolder}/{modelId}/{id.Trim('/')}.json");

        void LoadGroups() {
            var rawGroups = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(
                File.ReadAllText($"{PimixServiceJsonClient.DataFolder}/metadata/{modelId}/groups.json"));
            Groups.Clear();
            foreach (var rawGroup in rawGroups) {
                var group = rawGroup.Value;
                group.Insert(0, rawGroup.Key);
                foreach (var i in group) {
                    Groups[i] = group;
                }
            }
        }

        void SaveGroups() {
            var data = new Dictionary<string, List<string>>();
            foreach (var g in Groups) {
                if (g.Key == g.Value.First()) {
                    // TODO(C# 8): data[g.Key] = g.Value[1..^0];
                    data[g.Key] = g.Value.Skip(1).ToList();
                }
            }

            File.WriteAllText($"{PimixServiceJsonClient.DataFolder}/metadata/{modelId}/groups.json",
                JsonConvert.SerializeObject(data, Defaults.PrettyJsonSerializerSettings));
        }
    }
}
