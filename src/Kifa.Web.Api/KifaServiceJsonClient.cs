using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Kifa.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;

namespace Kifa.Web.Api;

public class KifaServiceJsonClient {
    public static string DefaultDataFolder {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    static readonly AsyncLocal<Dictionary<string, string?>?> dataFolders = new();

    public static Dictionary<string, string?> DataFolders {
        get => Late.Get(dataFolders.Value);
        set {
            var v = dataFolders.Value;
            Late.Set(ref v, value);
            dataFolders.Value = v;
        }
    }
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

            return matchedFolders.MaxBy(kv => kv.Key.Length).Value ??
                   KifaServiceJsonClient.DefaultDataFolder;
        }

        set => dataFolder = value;
    }

    #endregion

    static ConcurrentDictionary<string, Link<TDataModel>> Locks = new();

    protected static Link<TDataModel> GetLock(string id)
        => Locks.GetOrAdd(id.TryParseTranslationId(out var baseId, out _) ? baseId : id,
            key => key);

    protected static LockScope AcquireLock(string id, [CallerMemberName] string caller = "") {
        Logger.Trace($"Waiting for lock on {TDataModel.ModelId}/{id} in {caller}...");
        var lockObj = GetLock(id);
        Monitor.Enter(lockObj);
        Logger.Trace($"Acquired lock on {TDataModel.ModelId}/{id} in {caller}.");
        return new LockScope(lockObj, id, caller);
    }

    protected static IDisposable AcquireLocks(IEnumerable<string> ids,
        [CallerMemberName] string caller = "") {
        var sortedIds = ids.Distinct().OrderBy(id => id).ToList();
        var scopes = new List<LockScope>(sortedIds.Count);
        foreach (var id in sortedIds) {
            scopes.Add(AcquireLock(id, caller));
        }

        return new MultiLockScope(scopes);
    }

    protected readonly struct LockScope : IDisposable {
        readonly object lockObj;
        readonly string id;
        readonly string caller;

        public LockScope(object lockObj, string id, string caller) {
            this.lockObj = lockObj;
            this.id = id;
            this.caller = caller;
        }

        public void Dispose() {
            Monitor.Exit(lockObj);
            Logger.Trace($"Released lock on {TDataModel.ModelId}/{id} in {caller}.");
        }
    }

    protected readonly struct MultiLockScope : IDisposable {
        readonly List<LockScope> scopes;

        public MultiLockScope(List<LockScope> scopes) {
            this.scopes = scopes;
        }

        public void Dispose() {
            for (var i = scopes.Count - 1; i >= 0; i--) {
                scopes[i].Dispose();
            }
        }
    }

    public override SortedDictionary<string, TDataModel> List(string folder = "",
        bool recursive = true, KifaDataOptions? options = null) {
        // No data is gonna change. With no locking, the worst case is data not consistent.
        var prefix = $"{DataFolder}/{ModelId}";
        var subFolder = $"{prefix}/{folder.Trim('/')}";
        var virtualItemPrefix = $"{prefix}{DataModel.VirtualItemPrefix}";
        if (File.Exists(subFolder + ".json")) {
            Logger.Trace($"{subFolder} is actually a file. Return one element instead.");
            var data = Get(subFolder[prefix.Length..], options);
            return new SortedDictionary<string, TDataModel> {
                { data.Id, data }
            };
        }

        if (!Directory.Exists(subFolder)) {
            return new SortedDictionary<string, TDataModel>();
        }

        Logger.Trace($"List folder {subFolder}");

        var directory = new DirectoryInfo(subFolder);

        // We actually exclude virtual items twice here.
        // Supposedly only the first one is used. However, we should not rely on file naming. The
        // first one is only used to speed up query.
        // If folder is not null, we don't skip virtual items as it's either requested or won't be
        // included.
        var items = directory
            .GetFiles("*.json",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Where(p => !IsTranslationFile(p.Name))
            .Where(p => folder != "" || !p.FullName.StartsWith(virtualItemPrefix)).AsParallel()
            .Select(i => Read(i.FullName[prefix.Length..^5], checkId: false,
                language: options?.Translated)).ExceptNull()
            .Where(i => folder != "" || !i.Id.StartsWith(DataModel.VirtualItemPrefix))
            .Where(i => i.Metadata?.Status != DataStatus.NotFound)
            .ToDictionary(i => i.Id, i => i);

        return new SortedDictionary<string, TDataModel>(items.AsParallel().ToDictionary(i => i.Key,
            i => {
                if (i.Value.Metadata?.Linking?.Target == null) {
                    return FormatData(i.Value, options, skipId: true);
                }

                var target = i.Value.Metadata.Linking.Target;

                Logger.Trace($"Get value for {target}");

                var value = items.TryGetValue(target, out var item)
                    ? item.Clone()
                    : Read(target, language: options?.Translated).Checked();

                if (value.Metadata?.Linking?.Links?.Contains(i.Key) != true &&
                    value.Metadata?.Linking?.VirtualLinks?.Contains(i.Key) != true) {
                    throw new DataCorruptedException(
                        $"Target {target} does not contain link {i.Key}.");
                }

                // source.Metadata.Linking has to exist, to contain Links at least.
                value.Metadata.Checked().Linking.Checked().Target = value.Id;
                value.Id = i.Key;
                return FormatData(value, options, skipId: true);
            }));
    }

    public override TDataModel? Get(string id, KifaDataOptions? options = null) {
        try {
            TDataModel? data;
            using (AcquireLock(id)) {
                if (id.TryParseTranslationId(out var baseId, out var language)) {
                    if (!IsValidTranslationLanguage(language)) {
                        Logger.Warn(
                            $"Rejected Get for invalid translation language '{language.Code}' in {ModelId}/{id}.");
                        return null;
                    }

                    var baseItem = Retrieve(baseId);
                    if (baseItem == null || baseItem.Metadata?.Status == DataStatus.NotFound) {
                        return null;
                    }

                    var targetId = baseItem.RealId!;
                    using (AcquireLock(targetId)) {
                        if (baseItem.Metadata?.Translations?.Contains(language) != true) {
                            return null;
                        }

                        var translation = ReadTranslation(targetId, language);
                        if (translation == null) {
                            return null;
                        }

                        if (targetId != baseId) {
                            translation.Metadata ??= new DataMetadata();
                            translation.Metadata.Linking = new LinkingMetadata {
                                Target = targetId.GetTranslationId(language)
                            };
                            translation.Id = baseId.GetTranslationId(language);
                        }

                        return FormatData(translation, options);
                    }
                }

                data = Retrieve(id);

                if (Fill(ref data, id, options)) {
                    WriteTarget(data);
                } else if (options?.Rewrite == true) {
                    if (data != null) {
                        var rawData = ReadRaw(data.RealId);
                        if (rawData != null) {
                            var expectedRawData = $"{ToTargetDiskJson(data)}\n";
                            if (rawData != expectedRawData) {
                                Logger.Info(
                                    $"Rewriting {ModelId}/{data.RealId} as its JSON layout on disk did not match the expected structure.");
                                WriteTarget(data);
                            }
                        }
                    }
                }

                if (data == null || data.Metadata?.Status == DataStatus.NotFound) {
                    return null;
                }

                ReadAndFillTranslation(ref data, options?.Translated);
            }

            // Run outside AcquireLock(id): all disk I/O for `id` is complete, and LinkedFields expansion
            // fetches other items — holding lock(id) while locking linked targets risks AB-BA deadlocks
            // across concurrent requests (e.g. A -> B vs B -> A) and blocks other callers on `id`.
            return FormatData(data, options);
        } catch (Exception ex) when (ex is not DataCorruptedException and not ArgumentException) {
            Logger.Error(ex, $"Failed to get {id}.");
            return null;
        }
    }

    public override List<TDataModel?> Get(List<string> ids, KifaDataOptions? options = null)
        => ids.AsParallel().AsOrdered().Select(id => Get(id, options: options)).ToList();

    // false -> no write needed.
    // true -> rewrite needed.
    // `forceFill` runs Fill() even if the item is still fresh, e.g. when an update changed its content.
    bool Fill([NotNullWhen(true)] ref TDataModel? data, string? id = null,
        KifaDataOptions? options = null, bool forceFill = false) {
        var candidate = data ?? new TDataModel {
            Id = id
        };

        var input = options?.FillInput;
        if (candidate.RefreshInterval == RefreshIntervals.None) {
            if (input != null) {
                throw new ArgumentException(
                    $"{ModelId} is not fillable, so it cannot take fill input.");
            }

            return false;
        }

        var originalContent = data?.Clone();
        data = candidate;

        var refresh = options?.Refresh == true;
        var full = options?.Full == true;

        // Input always runs Fill(): the model decides what to do with it, and Version only moves
        // if the resulting content differs from what is stored.
        if (refresh || full || forceFill || input != null || data.NeedRefresh()) {
            var isNewItem = data.Metadata?.Version == null;
            var now = DateTimeOffset.UtcNow;

            try {
                data.Fill(new FillContext {
                    Full = full,
                    Input = input
                });
            } catch (DataIsLinkedException ex) {
                if (originalContent != null) {
                    Logger.Error(
                        $"Cannot link {ModelId}/{data.Id} to {ex.TargetId} as {data.Id} ({originalContent.RealId}) already exists.");
                    data = originalContent;
                    return false;
                }

                Get(ex.TargetId, options);
                if (Link(ex.TargetId, data.Id).Status != KifaActionStatus.OK) {
                    data = originalContent;
                    return false;
                }

                data = Retrieve(data.Id);

                // New data is written in Link. No need to save more.
                return false;
            } catch (NoNeedToFillException) {
                data = originalContent;
                return false;
            } catch (DataNotFoundException ex) {
                Logger.Warn(ex, $"{ModelId}/{data.Id} was not found upstream.");
                data = originalContent ?? new TDataModel {
                    Id = id
                };
                data.Metadata ??= new DataMetadata();
                var isEmpty = data.Metadata.Version == null && data.IsEmpty();
                if (isEmpty || data.Metadata.Status == DataStatus.NotFound) {
                    data.Metadata.Status = DataStatus.NotFound;
                    data.Metadata.Version = now;
                    data.Metadata.LastRefreshed = now;
                } else {
                    data.Metadata.Status = DataStatus.Removed;
                    data.Metadata.Version ??= now;
                    data.Metadata.LastRefreshed = now;
                }

                return true;
            } catch (FailedToFillException ex) {
                Logger.Warn(ex, $"Failed to fill {ModelId}/{data.Id} during process.");
                data = originalContent;
                return false;
            } catch (Exception ex) {
                Logger.Error(ex, $"Failed to fill {ModelId}/{data.Id} with an unexpected error.");
                data = originalContent;
                return false;
            }

            var contentChanged = isNewItem ||
                                 originalContent?.Metadata?.Status != DataStatus.OK ||
                                 !data.Equals(originalContent);

            data.Metadata ??= new DataMetadata();
            data.Metadata.Status = DataStatus.OK;
            data.Metadata.LastRefreshed = now;

            if (contentChanged) {
                data.Metadata.Version = now;
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
            var target = data.Metadata.Linking.Target;
            data = Read(target);
            if (data == null ||
                (data.Metadata?.Linking?.Links?.Contains(id) != true &&
                 data.Metadata?.Linking?.VirtualLinks?.Contains(id) != true)) {
                throw new DataCorruptedException(
                    $"Target {target} does not contain link {id}.");
            }

            data.Metadata.Linking.Target = data.Id;
            data.Id = id;
        }

        Logger.Trace($"Got {ModelId}/{id}");
        Logger.Notice(() => $"Content: {data}");
        return data;
    }

    public override KifaActionResult Set(TDataModel data) {
        if (data.Id.TryParseTranslationId(out var baseId, out var language)) {
            if (!IsValidTranslationLanguage(language)) {
                return LogAndReturn(new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message =
                        $"Cannot Set invalid translation language '{language.Code}' for {ModelId}/{data.Id}."
                });
            }

            return KifaActionResult.FromAction(() => {
                using (AcquireLock(baseId)) {
                    Logger.Trace($"Set translation {ModelId}/{data.Id}");
                    Logger.Notice(() => data.ToString());
                    var existing = Retrieve(baseId);
                    if (existing == null) {
                        throw new DataNotFoundException(
                            $"Cannot set translation for non-existent item {ModelId}/{baseId}.");
                    }

                    var targetId = existing.RealId!;
                    using (AcquireLock(targetId)) {
                        RegisterTranslationOnTarget(existing, language);
                        WriteTranslation(data, targetId, language);
                    }
                }
            });
        }

        return KifaActionResult.FromAction(() => {
            using (AcquireLock(data.Id)) {
                Logger.Trace($"Set {ModelId}/{data.Id}");
                Logger.Notice(() => data.ToString());
                var existing = Retrieve(data.Id);
                data = data.Clone();
                // This is new data, we should Fill it.
                var originalMetadata = data.Metadata?.Clone();
                data.ResetRefreshDate();
                if (!Fill(ref data)) {
                    data.Metadata = originalMetadata;
                }

                if (existing?.Metadata?.Translations is { Count: > 0 } existingTranslations) {
                    data.Metadata ??= new DataMetadata();
                    data.Metadata.Translations = existingTranslations;
                }

                WriteTarget(data, existing?.Metadata?.Linking?.VirtualLinks);
            }
        });
    }

    void WriteVirtualItems(TDataModel data, SortedSet<string> originalVirtualLinks,
        bool checkAllVirtualLinks = false) {
        var virtualLinks = data.GetVirtualItems();
        if (virtualLinks.Count == 0 && originalVirtualLinks.Count == 0) {
            return;
        }

        using (AcquireLocks(virtualLinks.Union(originalVirtualLinks))) {
            var toRemoveLinks = originalVirtualLinks.Except(virtualLinks);
            toRemoveLinks.ForEach(Remove);

            IEnumerable<string> linksToCheck = checkAllVirtualLinks
                ? virtualLinks
                : virtualLinks.Except(originalVirtualLinks);

            // We should make sure each virtual link only links to one item.
            var alreadyLinkedItems = linksToCheck
                .Select(item => (Item: item, Target: Read(item)?.Metadata?.Linking?.Target))
                .Where(item => item.Target != null && item.Target != data.RealId).ToList();
            if (alreadyLinkedItems.Count > 0) {
                throw new VirtualItemAlreadyLinkedException(
                    $"Some virtual links already exist, but not for {data.RealId}: {alreadyLinkedItems.Select(item => $"{item.Item} => {item.Target}").JoinBy(", ")}");
            }

            var toWriteLinks = checkAllVirtualLinks
                ? virtualLinks.Where(item => Read(item)?.Metadata?.Linking?.Target != data.RealId)
                    .ToList()
                : virtualLinks.Except(originalVirtualLinks).ToList();

            toWriteLinks.ForEach(item => Write(new TDataModel {
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
    }

    public override KifaActionResult Update(TDataModel data) {
        if (data.Id.TryParseTranslationId(out var baseId, out var language)) {
            if (!IsValidTranslationLanguage(language)) {
                return LogAndReturn(new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message =
                        $"Cannot Update invalid translation language '{language.Code}' for {ModelId}/{data.Id}."
                });
            }

            return KifaActionResult.FromAction(() => {
                using (AcquireLock(baseId)) {
                    Logger.Trace($"Update translation {ModelId}/{data.Id}");
                    Logger.Notice(() => data.ToString());
                    var original = Retrieve(baseId);
                    if (original == null) {
                        throw new DataNotFoundException(
                            $"Cannot update translation for non-existent item {ModelId}/{baseId}.");
                    }

                    var targetId = original.RealId!;
                    using (AcquireLock(targetId)) {
                        RegisterTranslationOnTarget(original, language);
                        var existingTranslation = ReadTranslation(targetId, language);
                        data = existingTranslation != null
                            ? existingTranslation.MergeTranslation(data)
                            : data.Clone();
                        WriteTranslation(data, targetId, language);
                    }
                }
            });
        }

        return KifaActionResult.FromAction(() => {
            using (AcquireLock(data.Id)) {
                Logger.Trace($"Update {ModelId}/{data.Id}");
                Logger.Notice(() => data.ToString());
                var original = Retrieve(data.Id);

                // If it's new data, we should try Fill it.
                if (original == null) {
                    var originalMetadata = data.Metadata?.Clone();
                    data.ResetRefreshDate();
                    if (!Fill(ref data)) {
                        data.Metadata = originalMetadata;
                    }
                } else {
                    var merged = original.Merge(data);
                    var contentChanged = !merged.Equals(original);
                    data = merged;

                    var wasDue = data.NeedRefresh();

                    // Changed inputs may affect other fields, so Fill() even if still fresh.
                    Fill(ref data, forceFill: contentChanged);

                    // Unchanged content that was not due for Fill() was just confirmed current by the
                    // caller, so only LastRefreshed moves (Version tracks content and stays).
                    if (!contentChanged && !wasDue &&
                        data.RefreshInterval != RefreshIntervals.None &&
                        data.Metadata?.Version != null) {
                        data.Metadata.LastRefreshed = DateTimeOffset.UtcNow;
                    }

                    // Version tracks the final content (after Fill) compared to what was stored.
                    if (contentChanged && !data.Equals(original)) {
                        data.Metadata ??= new DataMetadata();
                        data.Metadata.Version = DateTimeOffset.UtcNow;
                    }
                }

                WriteTarget(data);
            }
        });
    }

    // Serializes data as the target object on disk without mutating the caller's link view.
    static string ToTargetDiskJson(TDataModel data) {
        var originalId = data.Id;
        var originalTarget = data.Metadata?.Linking?.Target;
        try {
            data.Id = data.RealId;
            if (data.Metadata?.Linking != null) {
                data.Metadata.Linking.Target = null;
            }

            return data.ToDiskJson();
        } finally {
            data.Id = originalId;
            if (data.Metadata?.Linking != null) {
                data.Metadata.Linking.Target = originalTarget;
            }
        }
    }

    public override KifaActionResult Delete(string id) {
        if (id.TryParseTranslationId(out var baseId, out var language)) {
            if (!IsValidTranslationLanguage(language)) {
                return LogAndReturn(new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message =
                        $"Cannot Delete invalid translation language '{language.Code}' for {ModelId}/{id}."
                });
            }

            using (AcquireLock(baseId)) {
                var item = Retrieve(baseId);
                if (item == null || item.Metadata?.Translations?.Contains(language) != true) {
                    return LogAndReturn(new KifaActionResult {
                        Status = KifaActionStatus.BadRequest,
                        Message = $"Cannot find item {ModelId}/{id}"
                    });
                }

                var targetId = item.RealId!;
                using (AcquireLock(targetId)) {
                    item.Metadata.Translations.Remove(language);
                    if (item.Metadata.Translations.Count == 0) {
                        item.Metadata.Translations = null;
                    }

                    WriteRaw($"{ToTargetDiskJson(item)}\n", targetId);
                    Remove(targetId.GetTranslationId(language));
                    return KifaActionResult.Success();
                }
            }
        }

        using (AcquireLock(id)) {
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
                        MoveTranslations(id, nextItem.Id!, item.Metadata?.Translations);

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
                        var virtualLinks = linking.VirtualLinks ?? new SortedSet<string>();
                        using (AcquireLocks(virtualLinks)) {
                            foreach (var link in virtualLinks) {
                                Remove(link);
                            }
                        }

                        RemoveTranslations(id, item.Metadata?.Translations);
                    }
                } else {
                    linking.Links!.Remove(id);

                    WriteTarget(item);
                }
            } else {
                RemoveTranslations(id, item.Metadata?.Translations);
            }

            Remove(id);
            return KifaActionResult.Success();
        }
    }

    public override KifaActionResult Link(string targetId, string linkId) {
        if (targetId.TryParseTranslationId(out _, out _) ||
            linkId.TryParseTranslationId(out _, out _)) {
            return LogAndReturn(new KifaActionResult {
                Status = KifaActionStatus.BadRequest,
                Message =
                    $"Cannot Link translation sidecar IDs ({ModelId}/{targetId}, {ModelId}/{linkId})."
            });
        }
        using (AcquireLock(linkId)) {
            using (AcquireLock(targetId)) {
                var target = Retrieve(targetId);
                var link = Retrieve(linkId);

                if (target == null) {
                    return LogAndReturn(new KifaActionResult {
                        Status = KifaActionStatus.BadRequest,
                        Message = $"Target {targetId} doesn't exist."
                    });
                }

                var realTargetId = target.RealId;

                using (AcquireLock(realTargetId)) {
                    if (link != null) {
                        var realLinkId = link.RealId;
                        if (realLinkId == realTargetId) {
                            return LogAndReturn(new KifaActionResult {
                                Status = KifaActionStatus.Skipped,
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
                    return KifaActionResult.Success();
                }
            }
        }
    }

    TDataModel? Read(string id, bool checkId = true, Language? language = null) {
        var rawData = ReadRaw(id);
        Logger.Trace($"Read {id}");
        Logger.Notice(() => $"{rawData ?? "null"}");
        try {
            if (rawData == null) {
                return null;
            }

            // Using FromJson (Default settings) instead of FromDiskJson allows legacy files that
            // still have external properties embedded directly in the JSON to be read into the model
            // so they can be migrated and extracted into separate files on rewrite.
            var data = rawData.FromJson<TDataModel>().Checked();
            if (checkId ? data.Id != id : data.Id?.Trim('/') != id.Trim('/')) {
                throw new DataCorruptedException(
                    $"Data ID {data.Id} does not match requested ID {id}.");
            }

            if (data.Metadata?.Linking?.Target == null) {
                ReadAndFillExternalProperties(data);
                ReadAndFillTranslation(ref data, language);
            }

            return data;
        } catch (JsonReaderException ex) {
            Logger.Error(ex, $"Failed to read {ModelId}/{id}");
            throw;
        }
    }

    void Write(TDataModel data) {
        WriteRaw($"{data.ToDiskJson()}\n", data.Id);
    }

    const int MaxPathSegmentByteLength = 255;

    string GetPath(string id, string suffix = "json") {
        var trimmedId = id.Trim('/');
        var segments = trimmedId.Split('/');
        for (var i = 0; i < segments.Length; i++) {
            var segment = i == segments.Length - 1 ? $"{segments[i]}.{suffix}" : segments[i];
            var byteCount = Encoding.UTF8.GetByteCount(segment);
            if (byteCount > MaxPathSegmentByteLength) {
                throw new ArgumentException(
                    $"Path segment '{segment}' in ID '{id}' exceeds the maximum allowed length of {MaxPathSegmentByteLength} UTF-8 bytes (actual: {byteCount} bytes).",
                    nameof(id));
            }
        }

        return $"{DataFolder}/{ModelId}/{trimmedId}.{suffix}";
    }

    void WriteRaw(string content, string id, string suffix = "json") {
        var path = GetPath(id, suffix);
        MakeParent(path);
        File.WriteAllText(path, content);
    }

    void WriteTarget(TDataModel data, SortedSet<string>? originalVirtualLinks = null,
        bool checkAllVirtualLinks = false) {
        WriteVirtualItems(data,
            originalVirtualLinks ??
            data.Metadata?.Linking?.VirtualLinks ?? new SortedSet<string>(),
            checkAllVirtualLinks);
        WriteExternalProperties(data);
        WriteRaw($"{ToTargetDiskJson(data)}\n", data.RealId);
    }

    void WriteExternalProperties(TDataModel data) {
        foreach (var (property, suffix) in TDataModel.ExternalProperties) {
            if (property.GetValue(data) is string content) {
                WriteRaw(content, data.RealId, suffix);
            }
        }
    }

    void ReadAndFillExternalProperties(TDataModel data) {
        foreach (var (property, suffix) in TDataModel.ExternalProperties) {
            var content = ReadRaw(data.Id, suffix);
            if (content != null) {
                property.SetValue(data, content);
            }
        }
    }

    void ReadAndFillTranslation(ref TDataModel data, Language? language) {
        if (language != null && language != TDataModel.DefaultLanguage &&
            data.Metadata?.Translations?.Contains(language) == true && data.RealId != null) {
            var translation = ReadTranslation(data.RealId, language);
            if (translation != null) {
                data = data.MergeTranslation(translation);
            }
        }
    }

    string? ReadRaw(string id, string suffix = "json") {
        var path = GetPath(id, suffix);
        return !File.Exists(path) ? null : File.ReadAllText(path);
    }

    static bool IsValidTranslationLanguage(Language language)
        => language != Language.Unknown && language != TDataModel.DefaultLanguage;

    static bool IsTranslationFile(string fileName)
        => fileName.EndsWith(".json", StringComparison.Ordinal) &&
           fileName[..^5].TryParseTranslationId(out _, out _);

    TDataModel? ReadTranslation(string targetId, Language language) {
        var translationId = targetId.GetTranslationId(language);
        var rawData = ReadRaw(translationId);
        Logger.Trace($"Read translation {translationId}");
        try {
            if (rawData == null) {
                return null;
            }

            var data = rawData.FromJson<TDataModel>().Checked();
            if (data.Id?.Trim('/') != translationId.Trim('/')) {
                throw new DataCorruptedException(
                    $"Translation data ID {data.Id} does not match requested ID {translationId}.");
            }

            data.Metadata ??= new DataMetadata();
            data.Metadata.Translated = language;

            return data;
        } catch (JsonReaderException ex) {
            Logger.Error(ex, $"Failed to read translation {ModelId}/{translationId}");
            throw;
        }
    }

    void RegisterTranslationOnTarget(TDataModel targetItem, Language language) {
        targetItem.Metadata ??= new DataMetadata();
        targetItem.Metadata.Translations ??= [];
        if (targetItem.Metadata.Translations.Add(language)) {
            WriteRaw($"{ToTargetDiskJson(targetItem)}\n", targetItem.RealId);
        }
    }

    void WriteTranslation(TDataModel data, string targetId, Language language) {
        var translationId = targetId.GetTranslationId(language);
        var originalId = data.Id;
        var originalMetadata = data.Metadata;
        try {
            data.Id = translationId;
            data.Metadata = null;
            Write(data);
        } finally {
            data.Id = originalId;
            data.Metadata = originalMetadata;
        }
    }

    void MoveTranslations(string fromId, string toId, SortedSet<Language>? translations) {
        if (translations == null) {
            return;
        }

        foreach (var language in translations) {
            var fromTranslationId = fromId.GetTranslationId(language);
            var rawData = ReadRaw(fromTranslationId);
            if (rawData != null) {
                var data = rawData.FromJson<TDataModel>().Checked();
                WriteTranslation(data, toId, language);
            }

            Remove(fromTranslationId);
        }
    }

    void RemoveTranslations(string id, SortedSet<Language>? translations) {
        if (translations == null) {
            return;
        }

        foreach (var language in translations) {
            Remove(id.GetTranslationId(language));
        }
    }

    void Remove(string id) {
        var path = GetPath(id);
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

    static TDataModel? FormatData(TDataModel? data, KifaDataOptions? options, bool skipId = false) {
        if (data == null || options == null) {
            return data;
        }

        if (options.Fields.Count > 0) {
            var newValue = skipId
                ? new TDataModel()
                : new TDataModel {
                    Id = data.Id
                };
            foreach (var field in options.Fields) {
                if (field == nameof(data.Id)) {
                    continue;
                }

                var prop = TDataModel.AllProperties[field];
                prop.SetValue(newValue, prop.GetValue(data));
            }

            data = newValue;
        }

        if (options.LinkedFields.Count > 0) {
            List<object> rootItems = [data];
            foreach (var linkedField in options.LinkedFields) {
                var segments = linkedField.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 0) {
                    throw new ArgumentException($"Invalid empty linked field '{linkedField}'.",
                        nameof(options));
                }

                PopulateLinkedField(typeof(TDataModel), rootItems, linkedField, segments, 0);
            }
        }

        return data;
    }

    static readonly SnakeCaseNamingStrategy SnakeCase = new();

    static void PopulateLinkedField(Type type, List<object> items, string linkedField,
        string[] segments, int index) {
        // Case 1: Dictionary — unwrap all non-null dictionary values and continue at the same segment index.
        if (GetGenericInterfaceArgument(type, typeof(IDictionary<,>), 1) is { } dictValueType) {
            var nextItems = new List<object>();
            foreach (IDictionary dict in items) {
                foreach (var value in dict.Values) {
                    if (value != null) {
                        nextItems.Add(value);
                    }
                }
            }

            PopulateLinkedField(dictValueType, nextItems, linkedField, segments, index);
            return;
        }

        // Case 2: Collection/List — flatten all non-null elements and continue at the same segment index.
        if (type != typeof(string) &&
            GetGenericInterfaceArgument(type, typeof(IEnumerable<>), 0) is { } elementType) {
            var nextItems = new List<object>();
            foreach (IEnumerable enumerable in items) {
                foreach (var item in enumerable) {
                    if (item != null) {
                        nextItems.Add(item);
                    }
                }
            }

            PopulateLinkedField(elementType, nextItems, linkedField, segments, index);
            return;
        }

        // Case 3: Link<TDataModel> — expand each link (loading its target DataModel) and, if more path
        // segments remain, continue traversing/validating into targetType at the current segment index.
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Link<>)) {
            var targetType = type.GetGenericArguments()[0];
            var loadedTargets = new List<object>(items.Count);
            if (items.Count > 0) {
                var expandMethod = type.GetMethod("Expand").Checked();
                foreach (var link in items) {
                    if (expandMethod.Invoke(link, null) is { } loaded) {
                        loadedTargets.Add(loaded);
                    }
                }
            }

            if (index < segments.Length) {
                PopulateLinkedField(targetType, loadedTargets, linkedField, segments, index);
            }

            return;
        }

        if (index >= segments.Length) {
            throw new ArgumentException(
                $"Linked field '{linkedField}' ends on non-Link type '{type.Name}'; end node must be a Link<T>.");
        }

        // Case 4: Regular object — resolve the property matching segments[index] (by PascalCase or snake_case),
        // collect its values across all current items, and advance to index + 1.
        var segment = segments[index];
        var prop = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p =>
                p.Name.Equals(segment, StringComparison.OrdinalIgnoreCase) ||
                SnakeCase.GetPropertyName(p.Name, false)
                    .Equals(segment, StringComparison.OrdinalIgnoreCase));
        if (prop == null) {
            throw new ArgumentException(
                $"Invalid linked field '{linkedField}': property '{segment}' was not found on type '{type.Name}'.");
        }

        var propValues = new List<object>(items.Count);
        foreach (var item in items) {
            var val = prop.GetValue(item);
            if (val != null) {
                propValues.Add(val);
            }
        }

        PopulateLinkedField(prop.PropertyType, propValues, linkedField, segments, index + 1);
    }

    static Type? GetGenericInterfaceArgument(Type type, Type genericInterfaceDefinition,
        int argIndex) {
        if (type.IsInterface && type.IsGenericType &&
            type.GetGenericTypeDefinition() == genericInterfaceDefinition) {
            return type.GetGenericArguments()[argIndex];
        }

        return type.GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceDefinition)
            ?.GetGenericArguments()[argIndex];
    }
}
