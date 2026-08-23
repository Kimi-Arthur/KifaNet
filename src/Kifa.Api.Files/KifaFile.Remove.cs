using System.Linq;
using Kifa.IO;
using Kifa.Service;

namespace Kifa.Api.Files;

public partial class KifaFile {
    public KifaActionResult RemoveInstance(bool removeLinkOnly = false) {
        var result = new KifaBatchActionResult();

        var fileExists = Exists();
        if (!Registered) {
            if (removeLinkOnly) {
                if (Allocated) {
                    FileInfoClient.RemoveLocation(Id, ToString());
                    return new KifaActionResult {
                        Status = KifaActionStatus.Warning,
                        Message = $"Unverified file link {this} removed."
                    };
                }

                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"File link {this} not found."
                };
            }

            return fileExists
                ? KifaActionResult.FromAction(Delete)
                : new KifaActionResult {
                    Status = KifaActionStatus.Warning,
                    Message = $"File {this} deleted, no entry found though."
                };
        }

        if (!removeLinkOnly) {
            result.Add($"Remove {this}", fileExists
                ? KifaActionResult.FromAction(Delete)
                : new KifaActionResult {
                    Status = KifaActionStatus.Warning,
                    Message = $"File {this} not found."
                });
        }

        result.Add(ToString(), FileInfoClient.RemoveLocation(Id, ToString()));

        var updatedInfo = FileInfoClient.Get(Id);
        if (updatedInfo != null && updatedInfo.Locations.Count == 0) {
            result.Add($"Remove empty registry entry {Id}", FileInfoClient.Delete(Id));
        }

        return result;
    }

    public KifaActionResult RemoveLogical(bool removeLinkOnly = false, bool force = false)
        => RemoveLogical(Id, removeLinkOnly, force);

    public static KifaActionResult RemoveLogical(string? id, bool removeLinkOnly = false,
        bool force = false) {
        if (string.IsNullOrEmpty(id)) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = "File ID is null. Skipped."
            };
        }

        var info = FileInfoClient.Get(id);
        if (info == null) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = $"File {id} is not found in registration. Skipped"
            };
        }

        var result = new KifaBatchActionResult();
        var links = info.GetAllLinks();
        links.Remove(info.Id.Checked());
        var onlyFile = links.Count == 0;

        if (!onlyFile) {
            var otherLocations = info.Locations.Count(kv
                => new KifaFile(kv.Key).Id != info.Id && kv.Value != null);
            if (otherLocations == 0) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Skipped,
                    Message =
                        $"{info.Id} has no other instances other than the one linked. This will result in effective loss of the file."
                };
            }
        }

        if (onlyFile && !force) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = $"Since {info.Id} is the last instance and force is not specified, we skipped removing it."
            };
        }

        if (!removeLinkOnly) {
            foreach (var location in info.Locations.Keys) {
                var file = new KifaFile(location);

                var toRemove = file.Id == info.Id && !file.IsCloud || onlyFile && force;
                if (!toRemove && (onlyFile || file.Id == info.Id)) {
                    toRemove = !file.Exists();
                }

                if (toRemove) {
                    if (file.Exists()) {
                        file.Delete();
                        result.Add($"Removal of file instance {file}", new KifaActionResult {
                            Status = KifaActionStatus.OK,
                            Message = $"File {file} deleted."
                        });
                    } else {
                        result.Add($"Removal of file instance {file}", new KifaActionResult {
                            Status = KifaActionStatus.Warning,
                            Message = $"File {file} not found."
                        });
                    }

                    result.Add($"Removal of location {location}",
                        FileInfoClient.RemoveLocation(info.Id, location));
                } else {
                    Logger.Debug(
                        $"File {file} is not removed as there are other file entries, like {links.FirstOrDefault()}");
                }
            }
        }

        result.Add($"Removal of file info {info.Id}", FileInfoClient.Delete(info.Id));
        return result;
    }
}
