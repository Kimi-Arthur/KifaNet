using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Web.Api {
    public class KifaServiceJsonClient {
        public static string DataFolder { get; set; }
    }

    public class KifaServiceJsonClient<TDataModel> : BaseKifaServiceClient<TDataModel>
        where TDataModel : DataModel, new() {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        Dictionary<string, List<string>> Groups { get; } = new();

        public override SortedDictionary<string, TDataModel> List() {
            var prefix = $"{KifaServiceJsonClient.DataFolder}/{ModelId}";
            if (!Directory.Exists(prefix)) {
                return new SortedDictionary<string, TDataModel>();
            }

            var directory = new DirectoryInfo(prefix);
            var items = directory.GetFiles("*.json", SearchOption.AllDirectories).Select(i => {
                using var reader = i.OpenText();
                return JsonConvert.DeserializeObject<TDataModel>(reader.ReadToEnd(), Defaults.JsonSerializerSettings);
            }).ExceptNull().ToDictionary(i => i.Id, i => i);

            return new SortedDictionary<string, TDataModel>(items.ToDictionary(i => i.Key, i => {
                if (i.Value.Metadata?.Id == null) {
                    return i.Value;
                }

                var value = items[i.Value.Metadata.Id].Clone();
                value.Metadata!.Id = value.Id; // source.Metadata has to exist, to contain Links at least.
                value.Id = i.Key;
                return value;
            }));
        }

        public override TDataModel? Get(string id) {
            logger.Trace($"Get {ModelId}/{id}");
            var data = Read(id);
            if (data == null) {
                return null;
            }

            if (data.Metadata?.Id != null) {
                data = Read(data.Metadata.Id);
                data.Metadata.Id = data.Id;
                data.Id = id;
            }

            logger.Trace($"Got {data}");
            return data;
        }

        public override KifaActionResult Set(TDataModel data) =>
            KifaActionResult.FromAction(() => {
                logger.Trace($"Write {ModelId}/{data.Id}:");
                logger.Trace(data);
                data = data.Clone();
                data.Metadata ??= Get(data.Id)?.Metadata;
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
            var item = Get(id);
            if (item?.Metadata != null) {
                var metadata = item.Metadata;
                if (metadata.Id == null) {
                    // This is source. Metadata.Links has to exist.
                    var nextItem = Get(item.Metadata!.Links!.First());
                    nextItem!.Metadata!.Links!.Remove(nextItem.Id);
                    nextItem.Metadata.Id = null;
                    Set(nextItem);
                    foreach (var link in nextItem.Metadata.Links) {
                        var linkedItem = Read(link)!;
                        linkedItem.Metadata!.Id = nextItem.Id;
                        Write(linkedItem);
                    }
                } else {
                    // This is link.
                    metadata.Links!.Remove(id);
                    if (metadata.Links.Count == 0) {
                        metadata.Links = null;
                    }

                    Set(item);
                }
            }

            Remove(item.Id);
            return KifaActionResult.Success;
        }

        public override KifaActionResult Link(string targetId, string linkId) {
            var target = Get(targetId);
            var link = Get(linkId);

            if (target.Id == null) {
                return LogAndReturn(new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Target {targetId} doesn't exist."
                });
            }

            var realTargetId = target.Metadata?.Id ?? target.Id;

            if (link?.Id != null) {
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

            Write(new TDataModel {
                Id = linkId,
                Metadata = new DataMetadata {
                    Id = realTargetId
                }
            });

            target.Metadata ??= new DataMetadata();
            target.Metadata.Links ??= new HashSet<string>();
            target.Metadata.Links.Add(linkId);
            target.Id = realTargetId;
            target.Metadata.Id = null;
            Write(target);
            return KifaActionResult.Success;
        }

        public override KifaActionResult Refresh(string id) {
            var value = Get(id);
            value.Fill();
            return Set(value);
        }

        TDataModel? Read(string id) {
            var data = ReadRaw(id);
            return data == null
                ? null
                : JsonConvert.DeserializeObject<TDataModel>(data, Defaults.JsonSerializerSettings);
        }

        void Write(TDataModel data) {
            var path = $"{KifaServiceJsonClient.DataFolder}/{ModelId}/{data.Id.Trim('/')}.json";
            MakeParent(path);
            File.WriteAllText(path, JsonConvert.SerializeObject(data, Defaults.PrettyJsonSerializerSettings) + "\n");
        }

        string? ReadRaw(string id) {
            var path = $"{KifaServiceJsonClient.DataFolder}/{ModelId}/{id.Trim('/')}.json";
            return !File.Exists(path) ? null : File.ReadAllText(path);
        }

        void Remove(string id) {
            var path = $"{KifaServiceJsonClient.DataFolder}/{ModelId}/{id.Trim('/')}.json";
            File.Delete(path);
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
