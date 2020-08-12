using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Web.Api {
    public class PimixServiceJsonClient {
        public static string DataFolder { get; set; }
    }

    public class PimixServiceJsonClient<TDataModel> : BasePimixServiceClient<TDataModel> where TDataModel : DataModel {
        Dictionary<string, List<string>> Groups { get; } = new Dictionary<string, List<string>>();

        public override SortedDictionary<string, TDataModel> List() {
            var prefix = $"{PimixServiceJsonClient.DataFolder}/{modelId}";
            if (!Directory.Exists(prefix)) {
                return new SortedDictionary<string, TDataModel>();
            }

            var directory = new DirectoryInfo(prefix);
            var items = directory.GetFiles("*.json", SearchOption.AllDirectories);
            return new SortedDictionary<string, TDataModel>(items.Select(i =>
                    JsonConvert.DeserializeObject<TDataModel>(i.OpenText().ReadToEnd(),
                        Defaults.JsonSerializerSettings))
                .ToDictionary(i => i.Id, i => i));
        }

        public override TDataModel Get(string id) {
            LoadGroups();

            if (Groups.ContainsKey(id)) {
                var obj = JsonConvert.DeserializeObject<TDataModel>(Read(Groups[id].First()),
                    Defaults.JsonSerializerSettings);
                JsonConvert.PopulateObject(Read(id), obj);
                return obj;
            }

            return JsonConvert.DeserializeObject<TDataModel>(Read(id), Defaults.JsonSerializerSettings);
        }

        public override List<TDataModel> Get(List<string> ids) => ids.Select(Get).ToList();

        public override void Set(TDataModel data, string id = null) {
            id ??= data.Id;
            Save(id, data);
        }

        public override void Update(TDataModel data, string id = null) {
            id ??= data.Id;
            var original = Get(id);
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings), original);

            Save(id, original);
        }

        void Save(string id, TDataModel data) {
            var path = $"{PimixServiceJsonClient.DataFolder}/{modelId}/{id.Trim('/')}.json";
            MakeParent(path);
            File.WriteAllText(path, JsonConvert.SerializeObject(data, Defaults.PrettyJsonSerializerSettings) + "\n");
        }

        public override void Delete(string id) {
            throw new NotImplementedException();
        }

        public override void Link(string targetId, string linkId) {
            throw new NotImplementedException();
        }

        public override void Refresh(string id) {
            var value = Get(id);
            value.Fill();
            Set(value);
        }

        string Read(string id) {
            var path = $"{PimixServiceJsonClient.DataFolder}/{modelId}/{id.Trim('/')}.json";
            return !File.Exists(path) ? "{}" : File.ReadAllText(path);
        }

        void LoadGroups() {
            Groups.Clear();

            var groupsPath = $"{PimixServiceJsonClient.DataFolder}/metadata/{modelId}/groups.json";
            if (!File.Exists(groupsPath)) {
                return;
            }

            var rawGroups =
                JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(groupsPath));
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

        void MakeParent(string path) {
            Directory.CreateDirectory(path[..path.LastIndexOf('/')]);
        }
    }
}
