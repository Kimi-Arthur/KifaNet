using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.FileUtil;

public abstract class KifaFileCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static List<FileInformation> FindFileInfosByIds(IEnumerable<string> sources,
        bool recursive = true) {
        var fileIds = sources.SelectMany(f => FileInformation.Client.ListFolder(f, recursive))
            .Distinct().ToList();
        var infos = FileInformation.Client.Get(fileIds);
        return fileIds.Zip(infos).Select(item => item.Second ?? new FileInformation {
            Id = item.First
        }).ToList();
    }

    public static List<FileInformation> FindFileInfos(IEnumerable<string> sources, bool byId = false,
        bool recursive = true, string pattern = "*", bool ignoreFiles = true)
        => byId
            ? FindFileInfosByIds(sources, recursive)
            : KifaFile.FindPotentialFiles(sources, recursive, pattern, ignoreFiles)
                .Select(f => f.FileInfo ?? new FileInformation {
                    Id = f.Id
                }).ToList();

    public List<KifaFile> RegisterUnregisteredFiles(List<KifaFile> files,
        bool showSize = false, string actionVerb = "processing") {
        var notRegisteredFiles = files.Where(f => !f.Registered).ToList();
        if (notRegisteredFiles.Count == 0) {
            return files;
        }

        var toRegister = SelectMany(notRegisteredFiles,
            file => showSize && file.FileInfo?.Size != null
                ? $"{file} ({file.FileInfo.Size.ToSizeString()})"
                : file.ToString(),
            new Func<List<KifaFile>, string>(choices
                => $"files{(showSize ? $" ({choices.Sum(c => c.FileInfo?.Size ?? 0).ToSizeString()})" : "")} to register before {actionVerb}"));

        if (toRegister.Status == KifaActionStatus.OK) {
            toRegister.Value.ForEach(f => ExecuteItem($"register {f}", () => f.Add()));
        } else {
            ExecuteItem("files to register", () => toRegister);
        }

        return files.Where(f => f.Registered).ToList();
    }

    public KifaActionResult RemoveLogicalFile(FileInformation? info,
        bool removeLinkOnly = false, bool force = false) {
        var id = info?.Id;
        if (id == null) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = "File ID is null. Skipped."
            };
        }

        // We still need the latest info to decide for things like only file.
        info = FileInformation.Client.Get(id);
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
            // For non onlyFile, we need to check whether we will effectively lose the file.
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

        if (onlyFile && !force &&
            !Confirm($"{info.Id} is the last instance. Should it be removed?")) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = $"Since {info.Id} is the last instance, we skipped removing it."
            };
        }

        if (!removeLinkOnly) {
            foreach (var location in info.Locations.Keys) {
                var file = new KifaFile(location);

                // Do not auto remove remote file unless it's the last instance and force is
                // requested.
                var toRemove = file.Id == info.Id && !file.IsCloud || onlyFile && force;
                if (!toRemove) {
                    if (onlyFile || file.Id == info.Id) {
                        toRemove = !file.Exists() || Confirm(
                            $"Confirm removing dangling instance {file}, not matching file name and in cloud");
                    } else {
                        Logger.Debug(
                            $"File {file} is not removed as there are other file entries, like {links.First()}");
                    }
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
                        FileInformation.Client.RemoveLocation(info.Id, location));
                }
            }
        }

        // Logical removal.
        result.Add($"Removal of file info {info.Id}", FileInformation.Client.Delete(info.Id));
        return result;
    }

    public KifaActionResult RemoveFileInstance(KifaFile file, bool removeLinkOnly = false) {
        var result = new KifaBatchActionResult();

        var fileExists = file.Exists();
        if (!file.Registered) {
            if (removeLinkOnly) {
                if (file.Allocated) {
                    FileInformation.Client.RemoveLocation(file.Id, file.ToString());
                    return new KifaActionResult {
                        Status = KifaActionStatus.Warning,
                        Message = $"Unverified file link {file} removed."
                    };
                }

                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"File link {file} not found."
                };
            }

            return fileExists
                ? KifaActionResult.FromAction(file.Delete)
                : new KifaActionResult {
                    Status = KifaActionStatus.Warning,
                    Message = $"File {file} deleted, no entry found though."
                };
        }

        if (!removeLinkOnly) {
            result.Add($"Remove {file}", fileExists
                ? KifaActionResult.FromAction(file.Delete)
                : new KifaActionResult {
                    Status = KifaActionStatus.Warning,
                    Message = $"File {file} not found."
                });
        }

        result.Add(file.ToString(),
            FileInformation.Client.RemoveLocation(file.Id, file.ToString()));

        return result;
    }
}
