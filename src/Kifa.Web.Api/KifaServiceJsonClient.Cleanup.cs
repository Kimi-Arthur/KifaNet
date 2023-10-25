using System.Collections.Generic;
using System.Linq;
using Kifa.Service;

namespace Kifa.Web.Api;

public class FixOptions {
    // Fields to merge content. Currently only IDictionary is supported.
    public HashSet<string> FieldsToMerge { get; set; } = new();
}

public partial class KifaServiceJsonClient<TDataModel> {
    public void FixVirtualLinks(FixOptions? options) {
        var properties = typeof(TDataModel).GetProperties();

        options ??= new FixOptions();
        foreach (var item in List().Values) {
            try {
                WriteVirtualItems(item, new SortedSet<string>());
            } catch (VirtualItemAlreadyLinkedException ex) {
                Logger.Warn(ex,
                    $"Item {item.Id} ({item.RealId})'s virtual link is already linked.");
                var virtualLinks = item.GetVirtualItems();
                foreach (var virtualLink in virtualLinks) {
                    var currentLink = Get(virtualLink).Checked();
                    Logger.Debug(
                        $"Trying to merge {item.RealId} with {currentLink.RealId} due to {virtualLink}");

                    foreach (var property in properties.Where(p => p.CanWrite)) {
                        switch (property.Name) {
                            case "Id":
                                continue;
                            case "Metadata": {
                                var newMetadata = (property.GetValue(item) as DataMetadata).Checked();
                                var oldMetadata =
                                    (property.GetValue(currentLink) as DataMetadata).Checked();
                                if (newMetadata.Linking.Checked().Links?.Count > 0) {
                                    oldMetadata.Linking.Checked().Links = new SortedSet<string>();
                                    foreach (var link in newMetadata.Linking.Checked().Links
                                                 .Checked()) {
                                        oldMetadata.Linking.Checked().Links.Checked().Add(link);
                                    }
                                }

                                continue;
                            }
                        }

                        var newValue = property.GetValue(item);
                        var oldValue = property.GetValue(currentLink);

                        if (newValue.ToJson() == oldValue.ToJson()) {
                            continue;
                        }

                        if (property.PropertyType is {
                                IsGenericType: true
                            } && options.FieldsToMerge.Contains(property.Name)) {
                            if (property.PropertyType.GetInterface(typeof(IDictionary<,>).Name) !=
                                null) {
                                var oldItems =
                                    property.GetValue(oldValue) as IDictionary<object, object?>;
                                var newItems =
                                    property.GetValue(newValue) as IDictionary<object, object?>;
                                if (oldItems == null || newItems == null) {
                                    property.SetValue(oldValue, oldItems ?? newItems);
                                    continue;
                                }

                                foreach (var newItem in newItems) {
                                    if (newItem.Value != null ||
                                        !oldItems.ContainsKey(newItem.Key)) {
                                        oldItems[newItem.Key] = newItem.Value;
                                    }
                                }
                            }
                        }

                        throw new VirtualItemAlreadyLinkedException(
                            $"{item.RealId} and {currentLink.RealId} have conflicting values for {property.Name}: {newValue} vs {oldValue}");
                    }
                }
            }
        }
    }
}
