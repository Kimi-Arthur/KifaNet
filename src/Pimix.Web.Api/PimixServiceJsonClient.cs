using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;
using Pimix.Service;

namespace Pimix.Web.Api {
    public class PimixServiceJsonClient {
        public static string DataFolder { get; set; }
    }

    public class PimixServiceJsonClient<TDataModel> : BasePimixServiceClient<TDataModel>
        where TDataModel : DataModel, new() {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        Dictionary<string, List<string>> Groups { get; } = new();

        public override SortedDictionary<string, TDataModel> List() {
            var prefix = $"{PimixServiceJsonClient.DataFolder}/{modelId}";
            if (!Directory.Exists(prefix)) {
                return new SortedDictionary<string, TDataModel>();
            }

            var directory = new DirectoryInfo(prefix);
            var items = directory.GetFiles("*.json", SearchOption.AllDirectories);
            return new SortedDictionary<string, TDataModel>(items.Select(i => {
                using var reader = i.OpenText();
                return JsonConvert.DeserializeObject<TDataModel>(reader.ReadToEnd(), Defaults.JsonSerializerSettings);
            }).ToDictionary(i => i.Id, i => i));
        }

        public override TDataModel Get(string id) {
            var data = Read(id);
            if (data.Metadata?.Id != null) {
                data = Read(data.Metadata.Id);
                data.Metadata.Id = data.Id;
                data.Id = id;
            }

            return data;
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
            var target = Get(targetId);
            var link = Get(linkId);

            if (target.Id == null) {
                logger.Warn($"Target {targetId} doesn't exist.");
                return;
            }

            var realTargetId = target.Metadata?.Id ?? target.Id;

            if (link.Id != null) {
                var realLinkId = link.Metadata?.Id ?? link.Id;
                if (realLinkId == realTargetId) {
                    logger.Info($"Link {linkId} ({realLinkId}) is already linked to {targetId} ({realTargetId}).");
                    return;
                }

                logger.Warn($"Both {linkId} ({realLinkId}) and {targetId} ({realTargetId}) have data populated.");
                return;
            }

            Set(new TDataModel {Id = linkId, Metadata = new DataMetadata {Id = realTargetId}});
            target.Metadata ??= new DataMetadata();
            target.Metadata.Links ??= new HashSet<string>();
            target.Metadata.Links.Add(linkId);
            target.Id = realTargetId;
            target.Metadata.Id = null;
            Set(target);
        }

        public override void Refresh(string id) {
            var value = Get(id);
            value.Fill();
            Set(value);
        }

        TDataModel Read(string id) {
            return JsonConvert.DeserializeObject<TDataModel>(ReadRaw(id), Defaults.JsonSerializerSettings);
        }

        string ReadRaw(string id) {
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
