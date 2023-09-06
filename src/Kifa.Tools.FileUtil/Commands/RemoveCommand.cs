using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
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

    public override int Execute() {
        FileNames = FileNames.ToList();
        var removalText = RemoveLinkOnly ? "" : " and remove them from file system";

        if (ById) {
            var files = new List<string>();
            foreach (var fileName in FileNames) {
                if (!fileName.StartsWith('/')) {
                    files.Clear();
                    break;
                }

                files.AddRange(FileInformation.Client.ListFolder(fileName, true));
            }

            if (files.Count > 0) {
                foreach (var file in files) {
                    Console.WriteLine(file);
                }

                if (Confirm($"Confirm deleting the {files.Count} files above{removalText}?")) {
                    files.ForEach(f => ExecuteItem(f, () => RemoveLogicalFile(f)));
                    return LogSummary();
                }

                Logger.Info("Action canceled.");
                return 2;
            }

            // We support relative paths or FileInformation ids.
            var foundFiles = KifaFile.FindAllFiles(FileNames);
            if (foundFiles.Count > 0) {
                // We will assume relative paths are used here.
                foreach (var foundFile in foundFiles) {
                    Console.WriteLine(foundFile.Id);
                }

                if (Confirm($"Confirm deleting the {foundFiles.Count} files above{removalText}?")) {
                    foundFiles.ForEach(
                        f => ExecuteItem(f.Id, () => RemoveLogicalFile(f.ToString())));
                    return LogSummary();
                }

                Logger.Info("Action canceled.");
                return 2;
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

    KifaActionResult RemoveLogicalFile(string filePath) {
        var info = FileInformation.Client.Get(filePath).Checked();
        var result = new KifaBatchActionResult();
        if (!RemoveLinkOnly) {
            foreach (var location in info.Locations.Keys) {
                var file = new KifaFile(location);
                var links = info.GetAllLinks();
                links.Remove(info.Id);
                var shouldRemoveOtherFiles = links.Count == 0;

                // Do not auto remove remote file no matter what.
                var toRemove = file.Path == info.Id && file.IsLocal;
                if (!toRemove) {
                    if (shouldRemoveOtherFiles || file.Path == info.Id) {
                        toRemove =
                            Confirm(
                                $"Confirm removing dangling instance {file}, not matching file name or not local");
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
