using System;
using System.Linq;
using Kifa.Service;

namespace Kifa.Api.Files;

public partial class KifaFile {
    public KifaActionResult RemoveInstance(bool removeLinkOnly = false, bool force = false,
        Func<string, bool, bool>? confirmPrompt = null) {
        var result = new KifaBatchActionResult();

        var info = FileInfoClient.Get(Id);
        var otherLocations =
            info?.Locations.Count(kv => kv.Key != ToString() && kv.Value != null) ?? 0;

        if (otherLocations == 0) {
            if (confirmPrompt != null) {
                if (!force) {
                    var firstConfirmed = confirmPrompt.Invoke(
                        $"File {this} is the only instance. Confirm removing it completely?",
                        false);
                    if (!firstConfirmed) {
                        return new KifaActionResult {
                            Status = KifaActionStatus.Skipped,
                            Message =
                                $"Since {this} is the last instance and force is not specified, we skipped removing it."
                        };
                    }

                    var secondConfirmed = confirmPrompt.Invoke(
                        $"File {this} will be permanently lost. Are you sure you want to proceed?",
                        true);
                    if (!secondConfirmed) {
                        return new KifaActionResult {
                            Status = KifaActionStatus.Skipped,
                            Message =
                                $"Skipped removing {this} after second confirmation was declined."
                        };
                    }

                    force = true;
                } else {
                    var confirmed = confirmPrompt.Invoke(
                        $"File {this} is the only instance. Confirm removing it completely?",
                        true);
                    if (!confirmed) {
                        return new KifaActionResult {
                            Status = KifaActionStatus.Skipped,
                            Message = $"Skipped removing {this} on user confirmation."
                        };
                    }
                }
            } else if (!force) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Skipped,
                    Message =
                        $"Since {this} is the last instance and force is not specified, we skipped removing it."
                };
            }
        }

        var fileExists = Exists();
        if (!Registered) {
            if (removeLinkOnly) {
                if (Allocated) {
                    Logger.Info($"Removing unverified file link {this} from {Id}...");
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

            if (fileExists) {
                Logger.Info($"Deleting file {this}...");
                Delete();
                return KifaActionResult.Success();
            }

            return new KifaActionResult {
                Status = KifaActionStatus.Warning,
                Message = $"File {this} deleted, no entry found though."
            };
        }

        if (!removeLinkOnly) {
            if (fileExists) {
                Logger.Info($"Deleting file instance {this}...");
                Delete();
                result.Add($"Remove {this}", KifaActionResult.Success());
            } else {
                result.Add($"Remove {this}", new KifaActionResult {
                    Status = KifaActionStatus.Warning,
                    Message = $"File {this} not found."
                });
            }
        }

        Logger.Info($"Removing location {this} from {Id}...");
        result.Add(ToString(), FileInfoClient.RemoveLocation(Id, ToString()));

        var updatedInfo = FileInfoClient.Get(Id);
        if (updatedInfo != null && updatedInfo.Locations.Count == 0) {
            Logger.Info($"Removing empty registry entry {Id} from Kifa service...");
            result.Add($"Remove empty registry entry {Id}", FileInfoClient.Delete(Id));
        }

        return result;
    }

    public KifaActionResult RemoveLogical(bool removeLinkOnly = false, bool force = false,
        Func<string, bool, bool>? confirmPrompt = null)
        => RemoveLogical(Id, removeLinkOnly, force, confirmPrompt);

    public static KifaActionResult RemoveLogical(string? id, bool removeLinkOnly = false,
        bool force = false, Func<string, bool, bool>? confirmPrompt = null) {
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
            Logger.Info(
                $"File info {info.Id} has other linked FileInfo entries that are kept: [{links.JoinBy(", ")}].");
            var otherLocations = info.Locations.Count(kv
                => new KifaFile(kv.Key).Id != info.Id && kv.Value != null);
            if (otherLocations == 0) {
                if (confirmPrompt != null) {
                    if (!force) {
                        var firstConfirmed = confirmPrompt.Invoke(
                            $"File {info.Id} has no other instances other than the one linked. Confirm removing it?",
                            false);
                        if (!firstConfirmed) {
                            return new KifaActionResult {
                                Status = KifaActionStatus.Skipped,
                                Message =
                                    $"{info.Id} has no other instances other than the one linked. This will result in effective loss of the file."
                            };
                        }

                        var secondConfirmed = confirmPrompt.Invoke(
                            $"File {info.Id} will be permanently lost. Are you sure you want to proceed?",
                            true);
                        if (!secondConfirmed) {
                            return new KifaActionResult {
                                Status = KifaActionStatus.Skipped,
                                Message =
                                    $"Skipped removing {info.Id} after second confirmation was declined."
                            };
                        }

                        force = true;
                    } else {
                        var confirmed = confirmPrompt.Invoke(
                            $"File {info.Id} has no other instances other than the one linked. Confirm removing it?",
                            true);
                        if (!confirmed) {
                            return new KifaActionResult {
                                Status = KifaActionStatus.Skipped,
                                Message = $"Skipped removing {info.Id} on user confirmation."
                            };
                        }
                    }
                } else if (!force) {
                    return new KifaActionResult {
                        Status = KifaActionStatus.Skipped,
                        Message =
                            $"{info.Id} has no other instances other than the one linked. This will result in effective loss of the file."
                    };
                }
            }
        }

        if (onlyFile) {
            if (confirmPrompt != null) {
                if (!force) {
                    var firstConfirmed = confirmPrompt.Invoke(
                        $"File {info.Id} is the only version. Confirm removing it completely?",
                        false);
                    if (!firstConfirmed) {
                        return new KifaActionResult {
                            Status = KifaActionStatus.Skipped,
                            Message =
                                $"Since {info.Id} is the last instance and force is not specified, we skipped removing it."
                        };
                    }

                    var secondConfirmed = confirmPrompt.Invoke(
                        $"File {info.Id} will be permanently lost. Are you sure you want to proceed?",
                        true);
                    if (!secondConfirmed) {
                        return new KifaActionResult {
                            Status = KifaActionStatus.Skipped,
                            Message =
                                $"Skipped removing {info.Id} after second confirmation was declined."
                        };
                    }

                    force = true;
                } else {
                    var confirmed = confirmPrompt.Invoke(
                        $"File {info.Id} is the only version. Confirm removing it completely?",
                        true);
                    if (!confirmed) {
                        return new KifaActionResult {
                            Status = KifaActionStatus.Skipped,
                            Message = $"Skipped removing {info.Id} on user confirmation."
                        };
                    }
                }
            } else if (!force) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Skipped,
                    Message =
                        $"Since {info.Id} is the last instance and force is not specified, we skipped removing it."
                };
            }
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
                        Logger.Info($"Deleting file instance {file}...");
                        file.Delete();
                        result.Add($"Removal of file instance {file}", new KifaActionResult {
                            Status = KifaActionStatus.OK,
                            Message = $"File {file} deleted."
                        });
                    } else {
                        Logger.Info($"File instance {file} not found on disk.");
                        result.Add($"Removal of file instance {file}", new KifaActionResult {
                            Status = KifaActionStatus.Warning,
                            Message = $"File {file} not found."
                        });
                    }

                    Logger.Info($"Removing location {location} from file info {info.Id}...");
                    result.Add($"Removal of location {location}",
                        FileInfoClient.RemoveLocation(info.Id, location));
                } else {
                    Logger.Info(
                        $"File instance {file} is not removed as it belongs to other file entries: [{links.JoinBy(", ")}]");
                }
            }
        }

        Logger.Info($"Removing file info {info.Id} from Kifa service...");
        result.Add($"Removal of file info {info.Id}", FileInfoClient.Delete(info.Id));
        if (!onlyFile) {
            Logger.Info(
                $"Removed file info {info.Id}, while keeping other linked FileInfo entries: [{links.JoinBy(", ")}].");
        }

        return result;
    }
}
