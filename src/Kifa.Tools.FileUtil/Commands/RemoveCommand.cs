using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("rm",
    HelpText =
        "Remove the FILE. Can be either logic path like: /Software/... or real path like: local:desk/Software....")]
class RemoveCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #region public late IEnumerable<string> FileNames { get; set; }

    IEnumerable<string>? fileNames;

    [Value(0, Required = true, HelpText = "Target file(s) to remove.")]
    public IEnumerable<string> FileNames {
        get => Late.Get(fileNames);
        set => Late.Set(ref fileNames, value);
    }

    #endregion

    [Option('i', "id", HelpText = "Delete file by id instead of just instances.")]
    public virtual bool ById { get; set; } = false;

    [Option('l', "link", HelpText = "Remove link only.")]
    public bool RemoveLinkOnly { get; set; }

    [Option('a', "all-references", HelpText = "Remove all references.")]
    public bool RemoveAllReferences { get; set; }

    [Option('f', "force",
        HelpText =
            "Force remove files even if no other instances exist. Only use when a file is actually removed.")]
    public bool Force { get; set; }

    public override int Execute(KifaTask? task = null) {
        FileNames = FileNames.ToList();
        var removalText = RemoveLinkOnly ? "" : " and remove them from file system";

        if (RemoveAllReferences && !ById) {
            Logger.Fatal("-a can only be used with -i");
            return 1;
        }

        if (ById) {
            var fileInfos = new List<FileInformation>();
            foreach (var fileName in FileNames) {
                if (!fileName.StartsWith('/')) {
                    fileInfos.Clear();
                    break;
                }

                fileInfos.AddRange(FileInformation.Client.List(fileName).Values);
            }

            if (fileInfos.Count > 0) {
                foreach (var file in fileInfos) {
                    Console.WriteLine(file);
                }

                if (!Confirm($"Confirm deleting the {fileInfos.Count} files above{removalText}?") ||
                    Force && !Confirm(
                        "Since --force is specified, files of the only instance will automatically be removed! It will truly remove files from everywhere!!! Do you want to continue?")) {
                    Logger.Info("Action canceled.");
                    return 2;
                }

                fileInfos.ForEach(f => ExecuteItem(f.Id, () => RemoveLogicalFile(f)));
                return LogSummary();
            }

            // We support relative paths or FileInformation ids.
            var foundFiles = KifaFile.FindAllFiles(FileNames);
            if (foundFiles.Count > 0) {
                foreach (var foundFile in foundFiles) {
                    Console.WriteLine(foundFile.Id);
                }

                if (!Confirm(
                        $"Confirm deleting the {foundFiles.Count} files above{removalText}?") ||
                    Force && !Confirm(
                        "Since --force is specified, files of the only instance will automatically be removed! It will truly remove files from everywhere!!! Do you want to continue?")) {
                    Logger.Info("Action canceled.");
                    return 2;
                }

                foundFiles.ForEach(f => ExecuteItem(f.Id, () => RemoveLogicalFile(f.FileInfo)));
                return LogSummary();
            }

            Logger.Fatal("No files found!");
            return 1;
        }

        var localFiles = KifaFile.FindExistingFiles(FileNames);

        if (localFiles.Count > 0) {
            foreach (var file in localFiles) {
                Console.WriteLine(file);
            }

            if (!Confirm(
                    $"Confirm deleting the {localFiles.Count} file instances above{removalText}?")) {
                Logger.Info("Action canceled.");
                return 2;
            }

            localFiles.ForEach(f => ExecuteItem(f.ToString(), () => RemoveFileInstance(f)));
        }

        var phantomFiles = KifaFile.FindPhantomFiles(FileNames);
        if (phantomFiles.Count > 0) {
            foreach (var file in phantomFiles) {
                Console.WriteLine(file);
            }

            if (!Confirm(
                    $"Confirm deleting the {phantomFiles.Count} phantom files above{removalText}?")) {
                Logger.Info("Action canceled.");
                return 2;
            }

            phantomFiles.ForEach(f => ExecuteItem(f.ToString(), () => RemoveFileInstance(f)));
        }

        return LogSummary();
    }

    KifaActionResult RemoveLogicalFile(FileInformation info) {
        var result = new KifaBatchActionResult();
        var links = info.GetAllLinks();
        links.Remove(info.Id);
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

        if (onlyFile && !Force &&
            !Confirm($"{info.Id} is the last instance. Should it be removed?")) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = $"Since {info.Id} is the last instance, we skipped removing it."
            };
        }

        if (!RemoveLinkOnly) {
            foreach (var location in info.Locations.Keys) {
                var file = new KifaFile(location);

                // Do not auto remove remote file unless it's the last instance and force is
                // requested.
                var toRemove = file.Id == info.Id && !file.IsCloud || onlyFile && Force;
                if (!toRemove) {
                    if (onlyFile || file.Id == info.Id) {
                        toRemove = !file.Exists() ||
                                   Confirm(
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

    KifaActionResult RemoveFileInstance(KifaFile file) {
        var result = new KifaBatchActionResult();

        var fileExists = file.Exists();
        if (!file.Registered) {
            if (RemoveLinkOnly) {
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

            file.Delete();

            return fileExists
                ? new KifaActionResult {
                    Status = KifaActionStatus.OK,
                    Message = $"File {file} deleted, no entry found though."
                }
                : new KifaActionResult {
                    Status = KifaActionStatus.Warning,
                    Message = $"File {file} not found."
                };
        }

        if (!RemoveLinkOnly) {
            file.Delete();
            result.Add(file.Id, fileExists
                ? new KifaActionResult {
                    Status = KifaActionStatus.OK,
                    Message = $"File {file} deleted."
                }
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
