using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Web.Api;

public class KifaServiceJsonClient {
    #region public late static string DefaultDataFolder { get; set; }

    static string? defaultDataFolder;

    public static string DefaultDataFolder {
        get => Late.Get(defaultDataFolder);
        set => Late.Set(ref defaultDataFolder, value);
    }

    #endregion

    #region public late static Dictionary<string, string> DataFolders { get; set; }

    [ThreadStatic]
    static Dictionary<string, string?>? dataFolders;

    public static Dictionary<string, string?> DataFolders {
        get => Late.Get(dataFolders);
        set => Late.Set(ref dataFolders, value);
    }

    #endregion
}

public partial class KifaServiceJsonClient<TDataModel> : BaseKifaServiceClient<TDataModel>
    where TDataModel : DataModel, WithModelId<TDataModel>, new() {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #region public string DataFolder { get; set; }

    string? dataFolder;

    public string DataFolder {
        get {
            if (dataFolder != null) {
                return dataFolder;
            }

            var matchedFolders = KifaServiceJsonClient.DataFolders
                .Where(kv => typeof(TDataModel).FullName!.StartsWith(kv.Key)).ToList();
            if (matchedFolders.Count == 0) {
                // This means the user has no access to this data.
                throw new DataModelNotFoundException();
            }

            return dataFolder = matchedFolders.MaxBy(kv => kv.Key.Length).Value ??
                                KifaServiceJsonClient.DefaultDataFolder;
        }

        set => dataFolder = value;
    }

    #endregion

    static ConcurrentDictionary<string, Link<TDataModel>> Locks = new();

    protected static Link<TDataModel> GetLock(string id) => Locks.GetOrAdd(id, key => key);

    public override SortedDictionary<string, TDataModel> List(string folder = "",
        bool recursive = true) {
        // No data is gonna change. With no locking, the worst case is data not consistent.
        var prefix = $"{DataFolder}/{ModelId}";
        var subFolder = $"{prefix}/{folder.Trim('/')}";
        var virtualItemPrefix = $"{prefix}{DataModel.VirtualItemPrefix}";
        if (File.Exists(subFolder + ".json")) {
            Logger.Trace($"{subFolder} is actually a file. Return one element instead.");
            var data = Get(subFolder[prefix.Length..]);
            return new SortedDictionary<string, TDataModel> {
                { data.Id, data }
            };
        }

        if (!Directory.Exists(subFolder)) {
            return new SortedDictionary<string, TDataModel>();
        }

        var directory = new DirectoryInfo(subFolder);

        // We actually exclude virtual items twice here.
        // Supposedly only the first one is used. However, we should not rely on file naming. The
        // first one is only used to speed up query.
        // If folder is not null, we don't skip virtual items as it's either requested or won't be
        // included.
        var items = directory
            .GetFiles("*.json",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Where(p => folder != "" || !p.FullName.StartsWith(virtualItemPrefix)).AsParallel()
            .Select(i => Read(i.FullName[prefix.Length..^5])).ExceptNull()
            .Where(i => folder != "" || !i.Id.StartsWith(DataModel.VirtualItemPrefix))
            .ToDictionary(i => i.Id, i => i);

        return new SortedDictionary<string, TDataModel>(items.AsParallel().ToDictionary(i => i.Key,
            i => {
                if (i.Value.Metadata?.Linking?.Target == null) {
                    return i.Value;
                }

                var target = i.Value.Metadata.Linking.Target;

                Logger.Trace($"Get value for {target}");

                var value = items.TryGetValue(target, out var item)
                    ? item.Clone()
                    : Read(target).Checked();

                // source.Metadata.Linking has to exist, to contain Links at least.
                value.Metadata.Checked().Linking.Checked().Target = value.Id;
                value.Id = i.Key;
                return value;
            }));
    }

    public override TDataModel? Get(string id, bool refresh = false) {
        lock (GetLock(id)) {
            try {
                var data = Retrieve(id);

                if (Fill(ref data, id, refresh)) {
                    WriteTarget(data.Clone());
                }

                // TODO: Not sure...
                return Retrieve(id);
            } catch (Exception ex) {
                Logger.Error(ex, $"Failed to get {id}.");
                return null;
            }
        }
    }

    // TODO: Use parallel when DataFolder setup issue is solved.
    public override List<TDataModel?> Get(List<string> ids) => ids.Select(id => Get(id)).ToList();

    // false -> no write needed.
    // true -> rewrite needed.
    bool Fill([NotNullWhen(true)] ref TDataModel? data, string? id = null, bool refresh = false) {
        data ??= new TDataModel {
            Id = id,
            Metadata = new DataMetadata {
                Freshness = new FreshnessMetadata {
                    NextRefresh = Date.Zero
                }
            }
        };

        if (refresh) {
            data.Metadata ??= new DataMetadata();
            data.Metadata.Freshness = new FreshnessMetadata {
                NextRefresh = Date.Zero
            };
        }

        if (data.NeedRefresh()) {
            DateTimeOffset? nextUpdate;
            try {
                nextUpdate = data.Fill();
            } catch (DataIsLinkedException ex) {
                Link(ex.TargetId, data.Id);

                data = Retrieve(data.Id);

                // New data is written in Link. No need to save more.
                return false;
            } catch (NoNeedToFillException) {
                if (data.Metadata?.Freshness != null) {
                    data.Metadata.Freshness = null;
                }

                return false;
            } catch (UnableToFillException ex) {
                Logger.Error(ex, $"Failed to fill {ModelId}/{data.Id} with a predefined error.");
                return false;
            } catch (Exception ex) {
                Logger.Error(ex, $"Failed to fill {ModelId}/{data.Id} with an unexpected error.");
                return false;
            }

            data.Metadata ??= new DataMetadata();
            data.Metadata.Version = data.CurrentVersion;
            if (nextUpdate != null) {
                data.Metadata.Freshness = new FreshnessMetadata {
                    NextRefresh = nextUpdate
                };
            } else {
                data.Metadata.Freshness = null;
            }

            return true;
        }

        return false;
    }

    TDataModel? Retrieve(string id) {
        Logger.Trace($"Get {ModelId}/{id}");
        var data = Read(id);
        if (data == null) {
            return null;
        }

        if (data.Metadata?.Linking?.Target != null) {
            data = Read(data.Metadata.Linking.Target);
            data.Metadata.Linking.Target = data.Id;
            data.Id = id;
        }

        Logger.Trace($"Got {ModelId}/{id}");
        return data;
    }

    public override KifaActionResult Set(TDataModel data)
        => KifaActionResult.FromAction(() => {
            lock (GetLock(data.Id)) {
                Logger.Trace($"Set {ModelId}/{data.Id}: {data}");
                data = data.Clone();
                // This is new data, we should Fill it.
                data.ResetRefreshDate();
                Fill(ref data);
                WriteTarget(data, Retrieve(data.Id)?.Metadata?.Linking?.VirtualLinks);
            }
        });

    void WriteVirtualItems(TDataModel data, SortedSet<string> originalVirtualLinks) {
        var virtualLinks = data.GetVirtualItems();
        if (virtualLinks.Count == 0 && originalVirtualLinks.Count == 0) {
            return;
        }

        var toAddLinks = virtualLinks.Except(originalVirtualLinks).ToList();
        var toRemoveLinks = originalVirtualLinks.Except(virtualLinks);

        toRemoveLinks.ForEach(Remove);

        // We should make sure each virtual link only links to one item.
        var alreadyLinkedItems = toAddLinks
            .Select(item => (Item: item, Read(item)?.Metadata?.Linking?.Target))
            .Where(item => item.Target != null && item.Target != data.RealId).ToList();
        if (alreadyLinkedItems.Count > 0) {
            throw new VirtualItemAlreadyLinkedException(
                $"Some virtual links already exist, but not for {data.RealId}: {alreadyLinkedItems.Select(item => $"{item.Item} => {item.Target}").JoinBy(", ")}");
        }

        toAddLinks.ForEach(item => Write(new TDataModel {
            Id = item,
            Metadata = new DataMetadata {
                Linking = new LinkingMetadata {
                    Target = data.RealId
                }
            }
        }));

        if (virtualLinks.Count > 0) {
            data.Metadata ??= new DataMetadata();
            data.Metadata.Linking ??= new LinkingMetadata();
            data.Metadata.Linking.VirtualLinks = virtualLinks;
        }
    }

    public override KifaActionResult Update(TDataModel data)
        => KifaActionResult.FromAction(() => {
            lock (GetLock(data.Id)) {
                Logger.Trace($"Update {ModelId}/{data.Id}: {data}");
                // If it's new data, we should try Fill it.
                var original = Retrieve(data.Id);
                if (original == null) {
                    data.Metadata = new DataMetadata {
                        Freshness = new FreshnessMetadata {
                            NextRefresh = Date.Zero
                        }
                    };
                } else {
                    data = original.Merge(data);
                }

                Fill(ref data);
                WriteTarget(data);
            }
        });

    // Cleans up data for writing.
    // It will convert to the target object to write as the links don't need to be updated anyway.
    // It will also remove the parts that only make sense for the links.
    void CleanupForWriting(TDataModel data) {
        data.Id = data.RealId;
        var linking = data.Metadata?.Linking;
        if (linking != null) {
            linking.Target = null;
            if (linking.Links?.Count == 0) {
                linking.Links = null;
            }

            if (linking.VirtualLinks?.Count == 0) {
                linking.VirtualLinks = null;
            }

            if (linking.Links == null && linking.VirtualLinks == null) {
                data.Metadata.Linking = null;
            }
        }

        if (data.Metadata?.IsEmpty == true) {
            data.Metadata = null;
        }
    }

    public override KifaActionResult Delete(string id) {
        lock (GetLock(id)) {
            var item = Retrieve(id);
            if (item == null) {
                return LogAndReturn(new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Cannot find item {ModelId}/{id}"
                });
            }

            if (item.IsVirtualItem()) {
                return LogAndReturn(new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Cannot delete virtual item {ModelId}/{id}."
                });
            }

            if (item.Metadata?.Linking != null) {
                var linking = item.Metadata.Linking;
                if (linking.Target == null) {
                    if (linking.Links != null) {
                        // It means there are still real items.
                        var nextItem = Retrieve(linking.Links!.First());
                        linking = nextItem!.Metadata!.Linking!;

                        var links = linking.Links!;
                        links.Remove(nextItem.Id!);
                        linking.Target = null;

                        WriteTarget(nextItem);

                        foreach (var link in links.Concat(linking.VirtualLinks ??
                                                          new SortedSet<string>())) {
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
                    linking.Links!.Remove(id);

                    WriteTarget(item);
                }
            }

            Remove(id);
            return KifaActionResult.Success;
        }
    }

    public override KifaActionResult Link(string targetId, string linkId) {
        lock (GetLock(linkId)) {
            lock (GetLock(targetId)) {
                var target = Retrieve(targetId);
                var link = Retrieve(linkId);

                if (target == null) {
                    return LogAndReturn(new KifaActionResult {
                        Status = KifaActionStatus.BadRequest,
                        Message = $"Target {targetId} doesn't exist."
                    });
                }

                var realTargetId = target.RealId;

                lock (GetLock(realTargetId)) {
                    if (link != null) {
                        var realLinkId = link.RealId;
                        if (realLinkId == realTargetId) {
                            return LogAndReturn(new KifaActionResult {
                                Status = KifaActionStatus.OK,
                                Message =
                                    $"Link {linkId} ({realLinkId}) is already linked to {targetId} ({realTargetId})."
                            });
                        }

                        return LogAndReturn(new KifaActionResult {
                            Status = KifaActionStatus.BadRequest,
                            Message =
                                $"Both {linkId} ({realLinkId}) and {targetId} ({realTargetId}) have data populated."
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
            }
        }
    }

    TDataModel? Read(string id) {
        var rawData = ReadRaw(id);
        Logger.Trace($"Read: {rawData ?? "null"}");
        try {
            if (rawData == null) {
                return null;
            }

            var data = JsonConvert.DeserializeObject<TDataModel>(rawData,
                KifaJsonSerializerSettings.Default).Checked();

            ReadAndFillExternalProperties(data);
            return data;
        } catch (JsonReaderException ex) {
            Logger.Error(ex, $"Failed to read {ModelId}/{id}");
            throw;
        }
    }

    void Write(TDataModel data) {
        WriteRaw($"{JsonConvert.SerializeObject(data, KifaJsonSerializerSettings.Pretty)}\n",
            data.Id);
    }

    void WriteRaw(string content, string id, string suffix = "json") {
        var path = $"{DataFolder}/{ModelId}/{id.Trim('/')}.{suffix}";
        MakeParent(path);
        File.WriteAllText(path, content);
    }

    void WriteTarget(TDataModel data, SortedSet<string>? originalVirtualLinks = null) {
        WriteVirtualItems(data,
            originalVirtualLinks ??
            data.Metadata?.Linking?.VirtualLinks ?? new SortedSet<string>());
        CleanupForWriting(data);
        WriteAndClearExternalProperties(data);
        Write(data);
    }

    static List<(PropertyInfo property, string Suffix)>? externalFields1;

    static List<(PropertyInfo property, string Suffix)> ExternalFields
        => externalFields1 ??= GatherExternalProperties();

    static List<(PropertyInfo property, string Suffix)> GatherExternalProperties() {
        var properties = new List<(PropertyInfo property, string Suffix)>();
        var type = typeof(TDataModel);
        foreach (var property in type.GetProperties()) {
            var attribute = property.GetCustomAttribute<ExternalPropertyAttribute>();
            if (attribute == null) {
                continue;
            }

            if (property.PropertyType != typeof(string)) {
                throw new InvalidExternalPropertyException(
                    $"Property {property} marked with {nameof(ExternalPropertyAttribute)} should be of type string, but is {property.PropertyType}.");
            }

            if (attribute.Suffix.EndsWith("json")) {
                throw new InvalidExternalPropertyException(
                    $"Property {property} marked with {nameof(ExternalPropertyAttribute)} should not use json as extension, but used {attribute.Suffix}.");
            }

            properties.Add((property, attribute.Suffix));
        }

        return properties;
    }

    void WriteAndClearExternalProperties(TDataModel data) {
        foreach (var (property, suffix) in ExternalFields) {
            if (property.GetValue(data) is string content) {
                WriteRaw(content, data.Id, suffix);
                property.SetValue(data, "");
            }
        }
    }

    void ReadAndFillExternalProperties(TDataModel data) {
        foreach (var (property, suffix) in ExternalFields) {
            var content = ReadRaw(data.Id, suffix);
            if (content != null) {
                property.SetValue(data, content);
            }
        }
    }

    string? ReadRaw(string id, string suffix = "json") {
        var path = $"{DataFolder}/{ModelId}/{id.Trim('/')}.{suffix}";
        return !File.Exists(path) ? null : File.ReadAllText(path);
    }

    void Remove(string id) {
        var path = $"{DataFolder}/{ModelId}/{id.Trim('/')}.json";
        try {
            File.Delete(path);
            Logger.Trace($"Deleted {path}");
        } catch (DirectoryNotFoundException ex) {
            Logger.Trace(ex, $"Folder not found for {path}. Skipped.");
        }
    }

    static KifaActionResult LogAndReturn(KifaActionResult actionResult) {
        Logger.Log(actionResult.Status switch {
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
