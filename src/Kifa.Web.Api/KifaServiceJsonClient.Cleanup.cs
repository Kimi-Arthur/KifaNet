using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;

namespace Kifa.Web.Api;

public class FixOptions {
    // Fields to merge content. Currently only IDictionary is supported.
    public HashSet<string> FieldsToMerge { get; set; } = new();
}

public partial class KifaServiceJsonClient<TDataModel> {
    public KifaActionResult FixVirtualLinks(FixOptions? options) {
        var properties = typeof(TDataModel).GetProperties();

        options ??= new FixOptions();
        var overallResult = new KifaBatchActionResult();
        foreach (var item in List().Values) {
            overallResult.Add(item.Id, KifaActionResult.FromAction(() => {
                try {
                    WriteVirtualItems(item, new SortedSet<string>());
                } catch (VirtualItemAlreadyLinkedException ex) {
                    Logger.Debug(ex,
                        $"Item {item.Id} ({item.RealId})'s virtual link is already linked.");
                    var virtualLinks = item.GetVirtualItems();
                    var targetItem = Get(virtualLinks.First()).Checked();
                    foreach (var virtualLink in virtualLinks.Skip(1)) {
                        var nextItem = Get(virtualLink).Checked();
                        if (targetItem.RealId != nextItem.RealId) {
                            throw new DataCorruptedException(
                                $"Virtually linked items should point to the same item {targetItem.RealId} != {nextItem.RealId}.");
                        }
                    }

                    Logger.Debug(
                        $"Trying to merge {item.RealId} with {targetItem.RealId} due to {virtualLinks.First()}");

                    foreach (var property in properties.Where(p => p.CanWrite)) {
                        switch (property.Name) {
                            case "Id":
                                continue;
                            case "Metadata": {
                                var newMetadata =
                                    (property.GetValue(item) as DataMetadata).Checked();
                                var oldMetadata = (property.GetValue(targetItem) as DataMetadata)
                                    .Checked();
                                oldMetadata.Linking.Checked().Links ??= new SortedSet<string>();
                                oldMetadata.Linking.Checked().Links.Checked().Add(item.Id);
                                if (newMetadata.Linking.Checked().Links?.Count > 0) {
                                    foreach (var link in newMetadata.Linking.Checked().Links
                                                 .Checked()) {
                                        oldMetadata.Linking.Checked().Links.Checked().Add(link);
                                    }
                                }

                                continue;
                            }
                        }

                        var newValue = property.GetValue(item);
                        var oldValue = property.GetValue(targetItem);

                        if (newValue.ToJson() == oldValue.ToJson()) {
                            continue;
                        }

                        if (options.FieldsToMerge.Contains(property.Name) &&
                            property.PropertyType is {
                                IsGenericType: true
                            } && property.PropertyType.GetInterface(typeof(IDictionary<,>).Name) !=
                            null) {
                            var oldItems = oldValue as IDictionary;
                            var newItems = newValue as IDictionary;
                            if (oldItems == null || newItems == null) {
                                Logger.Trace(
                                    $"{oldItems} or {newItems} is null. Directly set the other one.");
                                property.SetValue(targetItem, oldItems ?? newItems);
                                continue;
                            }

                            Logger.Trace($"Merging {oldItems.ToJson()} with {newItems.ToJson()}");

                            foreach (DictionaryEntry newItem in newItems) {
                                if (newItem.Value != null || !oldItems.Contains(newItem.Key)) {
                                    oldItems[newItem.Key] = newItem.Value;
                                }
                            }

                            Logger.Trace($"Result is {oldItems.ToJson()}");

                            continue;
                        }

                        throw new VirtualItemAlreadyLinkedException(
                            $"{item.RealId} and {targetItem.RealId} have conflicting values for {property.Name}: {newValue} vs {oldValue}");
                    }

                    WriteTarget(targetItem);

                    Write(new TDataModel {
                        Id = item.Id,
                        Metadata = new DataMetadata {
                            Linking = new LinkingMetadata {
                                Target = targetItem.RealId
                            }
                        }
                    });
                }
            }));
        }

        return overallResult;
    }
}
