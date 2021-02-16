using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;
using Kifa.Service;

namespace Kifa.Web.Api {
    public class KifaServiceJsonClient {
        public static string DataFolder { get; set; }
    }

    public class KifaServiceJsonClient<TDataModel> : BaseKifaServiceClient<TDataModel>
        where TDataModel : DataModel, new() {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        Dictionary<string, List<string>> Groups { get; } = new();

        public override SortedDictionary<string, TDataModel> List() {
            var prefix = $"{KifaServiceJsonClient.DataFolder}/{modelId}";
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

        public override KifaActionResult Set(TDataModel data) =>
            KifaActionResult.FromAction(() => {
                data.Metadata ??= Get(data.Id).Metadata;
                if (data.Metadata?.Id != null) {
                    // The data is linked.
                    data.Id = data.Metadata.Id;
                    data.Metadata.Id = null;
                }

                Write(data);
            });

        public override KifaActionResult Update(TDataModel data) =>
            KifaActionResult.FromAction(() => {
                var original = Get(data.Id);
                JsonConvert.PopulateObject(JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings),
                    original);
                if (data.Metadata?.Id != null) {
                    // The data is linked.
                    data.Id = data.Metadata.Id;
                    data.Metadata.Id = null;
                }

                Write(data);
            });

        public override KifaActionResult Delete(string id) {
            throw new NotImplementedException();
        }

        public override KifaActionResult Link(string targetId, string linkId) {
            var target = Get(targetId);
            var link = Get(linkId);

            if (target.Id == null) {
                return LogAndReturn(new KifaActionResult {
                    Status = KifaActionStatus.BadRequest, Message = $"Target {targetId} doesn't exist."
                });
            }

            var realTargetId = target.Metadata?.Id ?? target.Id;

            if (link.Id != null) {
                var realLinkId = link.Metadata?.Id ?? link.Id;
                if (realLinkId == realTargetId) {
                    return LogAndReturn(new KifaActionResult {
                        Status = KifaActionStatus.BadRequest,
                        Message = $"Link {linkId} ({realLinkId}) is already linked to {targetId} ({realTargetId})."
                    });
                }

                return LogAndReturn(new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Both {linkId} ({realLinkId}) and {targetId} ({realTargetId}) have data populated."
                });
            }

            Write(new TDataModel {Id = linkId, Metadata = new DataMetadata {Id = realTargetId}});

            target.Metadata ??= new DataMetadata();
            target.Metadata.Links ??= new HashSet<string>();
            target.Metadata.Links.Add(linkId);
            target.Id = realTargetId;
            target.Metadata.Id = null;
            Write(target);
            return KifaActionResult.SuccessActionResult;
        }

        public override KifaActionResult Refresh(string id) {
            var value = Get(id);
            value.Fill();
            return Set(value);
        }

        TDataModel Read(string id) {
            return JsonConvert.DeserializeObject<TDataModel>(ReadRaw(id), Defaults.JsonSerializerSettings);
        }

        void Write(TDataModel data) {
            var path = $"{KifaServiceJsonClient.DataFolder}/{modelId}/{data.Id.Trim('/')}.json";
            MakeParent(path);
            File.WriteAllText(path, JsonConvert.SerializeObject(data, Defaults.PrettyJsonSerializerSettings) + "\n");
        }

        string ReadRaw(string id) {
            var path = $"{KifaServiceJsonClient.DataFolder}/{modelId}/{id.Trim('/')}.json";
            return !File.Exists(path) ? "{}" : File.ReadAllText(path);
        }

        static KifaActionResult LogAndReturn(KifaActionResult actionResult) {
            logger.Log(actionResult.Status switch {
                KifaActionStatus.Error => LogLevel.Error,
                KifaActionStatus.BadRequest => LogLevel.Warn,
                KifaActionStatus.OK => LogLevel.Info,
                _ => LogLevel.Info
            }, actionResult.Message);
            return actionResult;
        }

        static void MakeParent(string path) {
            Directory.CreateDirectory(path[..path.LastIndexOf('/')]);
        }
    }
}
