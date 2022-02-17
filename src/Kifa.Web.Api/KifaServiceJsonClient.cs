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
            var virtualItemPrefix = $"{prefix}{DataModel.VirtualItemPrefix}";
            if (!Directory.Exists(prefix)) {
                return new SortedDictionary<string, TDataModel>();
            }

            var directory = new DirectoryInfo(prefix);

            // We actually exclude virtual items twice here.
            // Supposedly only the first one is used. However, we should not rely everything on file naming.
            var items = directory.GetFiles("*.json", SearchOption.AllDirectories)
                .Where(p => !p.FullName.StartsWith(virtualItemPrefix)).AsParallel().Select(i => {
                    using var reader = i.OpenText();
                    return JsonConvert.DeserializeObject<TDataModel>(reader.ReadToEnd(),
                        Defaults.JsonSerializerSettings);
                }).ExceptNull().Where(i => i.Id != null && !i.Id.StartsWith(DataModel.VirtualItemPrefix))
                .ToDictionary(i => i.Id!, i => i);

            return new SortedDictionary<string, TDataModel>(items.ToDictionary(i => i.Key, i => {
                if (i.Value.Metadata?.Linking?.Target == null) {
                    return i.Value;
                }

                var value = items[i.Value.Metadata.Linking.Target].Clone();
                // source.Metadata.Linking! has to exist, to contain Links at least.
                value.Metadata!.Linking!.Target = value.Id;
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

            if (data.Metadata?.Linking?.Target != null) {
                data = Read(data.Metadata.Linking.Target);
                data.Metadata.Linking.Target = data.Id;
                data.Id = id;
            }

            logger.Trace($"Got {data}");
            return data;
        }

        public override KifaActionResult Set(TDataModel data) =>
            KifaActionResult.FromAction(() => {
                logger.Trace($"Write {ModelId}/{data.Id}: {data}");
                data = data.Clone();
                WriteVirtualItems(data); // TODO: Should not write every time.
                CleanupForWriting(data);

                Write(data);
            });

        void WriteVirtualItems(TDataModel data) {
            var items = data.GetVirtualItems();
            if (items.Count > 0) {
                return;
            }

            data.Metadata ??= new DataMetadata();
            data.Metadata.Linking ??= new LinkingMetadata();
            data.Metadata.Linking.VirtualLinks ??= new SortedSet<string>();

            foreach (var item in items) {
                if (!data.Metadata.Linking.VirtualLinks.Contains(item)) {
                    data.Metadata.Linking.VirtualLinks.Add(item);
                    Write(new TDataModel {
                        Id = item,
                        Metadata = new DataMetadata {
                            Linking = new LinkingMetadata {
                                Target = data.RealId
                            }
                        }
                    });
                }
            }
        }

        public override KifaActionResult Update(TDataModel data) =>
            KifaActionResult.FromAction(() => {
                var original = Get(data.Id);
                JsonConvert.PopulateObject(JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings),
                    original);
                data = original;
                CleanupForWriting(data);

                Write(data);
            });

        // Cleans up data for writing.
        // It will convert to the target object to write as the links don't need to be updated anyway. It will also
        // remove the parts that only make sense for the links.
        void CleanupForWriting(TDataModel data) {
            logger.Trace($"Before cleanup: {data}");
            if (data.RealId != data.Id) {
                logger.Trace("The data is linked. Nothing to be updated for link.");
                data.Id = data.RealId;
                data.Metadata!.Linking!.Target = null;
                if (data.Metadata.Linking.Links?.Count == 0) {
                    data.Metadata.Linking.Links = null;
                }
                if (data.Metadata.Linking.VirtualLinks?.Count == 0) {
                    data.Metadata.Linking.VirtualLinks = null;
                }

                if (data.Metadata.Linking.Links == null && data.Metadata.Linking.VirtualLinks == null) {
                    data.Metadata.Linking = null;
                }
            }

            logger.Trace($"After cleanup: {data}");
        }

        public override KifaActionResult Delete(string id) {
            var item = Get(id);
            if (item == null) {
                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Cannot find item {ModelId}/{id}"
                };
            }

            if (item.IsVirtualItem()) {
                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Cannot delete virtual item {ModelId}/{id}."
                };
            }

            if (item.Metadata?.Linking != null) {
                var linking = item.Metadata.Linking;
                if (linking.Target == null) {
                    if (linking.Links != null) {
                        // It means there are still real items.
                        var nextItem = Get(linking.Links!.First());
                        linking = nextItem!.Metadata!.Linking!;

                        var links = linking.Links!;
                        links.Remove(nextItem.Id!);
                        if (links.Count == 0) {
                            linking.Links = null;
                            if (linking.VirtualLinks == null) {
                                // No other items left. No Linking needed.
                                nextItem.Metadata.Linking = null;
                            }
                        }

                        Set(nextItem);

                        foreach (var link in links.Concat(linking.VirtualLinks ?? new())) {
                            Write(new TDataModel {
                                Id = link,
                                Metadata = new DataMetadata {
                                    Linking = new LinkingMetadata {
                                        Target = nextItem.Id
                                    }
                                }
                            });
                        }
                    } else {
                        // All real items are gone. So virtual items should go too.
                        foreach (var link in linking.VirtualLinks!) {
                            Remove(link);
                        }
                    }
                } else {
                    linking.Target = null;
                    linking.Links!.Remove(id);
                    item.Id = item.RealId;

                    Set(item);
                }
            }

            Remove(id);
            return KifaActionResult.Success;
        }

        public override KifaActionResult Link(string targetId, string linkId) {
            var target = Get(targetId);
            var link = Get(linkId);

            if (target?.Id == null) {
                return LogAndReturn(new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Target {targetId} doesn't exist."
                });
            }

            var realTargetId = target.RealId;

            if (link?.Id != null) {
                var realLinkId = link.RealId;
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
                    Linking = new LinkingMetadata {
                        Target = realTargetId
                    }
                }
            });

            target.Metadata ??= new DataMetadata();
            target.Metadata.Linking ??= new LinkingMetadata();
            target.Metadata.Linking.Links ??= new SortedSet<string>();
            target.Metadata.Linking.Links.Add(linkId);
            target.Id = realTargetId;
            target.Metadata.Linking.Target = null;
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
