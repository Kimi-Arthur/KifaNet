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

    public IEnumerable<string> FileNames {
        get => Late.Get(fileNames);
        set => Late.Set(ref fileNames, value);
    }

    #endregion

    [Option('i', "id", HelpText = "Delete file by id instead of just instances.")]
    public virtual bool ById { get; set; } = false;

    [Option('l', "link", HelpText = "Remove link only.")]
    public bool RemoveLinkOnly { get; set; }

    [Option('f', "force",
        HelpText =
            "Remove all instances of the file, including file with different name and in cloud.")]
    public bool ForceRemove { get; set; }

    public override int Execute() {
        FileNames = FileNames.ToList();
        var removalText = RemoveLinkOnly ? "" : " and remove them from file system";

        if (ById) {
            // We support relative paths or FileInformation ids.
            var (_, foundFiles) = KifaFile.FindAllFiles(FileNames, fullFile: true);
            if (foundFiles.Count > 0) {
                // We will assume relative paths are used here.
                foreach (var foundFile in foundFiles) {
                    Console.WriteLine(foundFile.Id);
                }

                if (Confirm($"Confirm deleting the {foundFiles.Count} files above{removalText}?")) {
                    foundFiles.ForEach(f
                        => ExecuteItem(f.Id, () => RemoveLogicalFile(f.FileInfo!)));
                    return LogSummary();
                }

                Logger.Info("Action canceled.");
                return 2;
            }

            var files = new List<string>();
            foreach (var fileName in FileNames) {
                files.AddRange(FileInformation.Client.ListFolder(fileName, true));
            }

            if (files.Count > 0) {
                foreach (var file in files) {
                    Console.WriteLine(file);
                }

                if (Confirm($"Confirm deleting the {files.Count} files above{removalText}?")) {
                    files.ForEach(f
                        => ExecuteItem(f, () => RemoveLogicalFile(FileInformation.Client.Get(f)!)));
                    return LogSummary();
                }

                Logger.Info("Action canceled.");
                return 2;
            }

            Logger.Fatal("No files found!");
            return 1;
        }

        var (_, localFiles) = KifaFile.FindExistingFiles(FileNames);

        foreach (var file in localFiles) {
            Console.WriteLine(file);
        }

        if (Confirm(
                $"Confirm deleting the {localFiles.Count} file instances above{removalText}?")) {
            localFiles.ForEach(f => ExecuteItem(f.ToString(), () => RemoveFileInstance(f)));
            return LogSummary();
        }

        Logger.Info("Action canceled.");
        return 2;
    }

    KifaActionResult RemoveLogicalFile(FileInformation info) {
        var result = new KifaBatchActionResult();
        if (!RemoveLinkOnly && info.Locations != null) {
            foreach (var location in info.Locations.Keys) {
                var file = new KifaFile(location);

                var toRemove = file.Id == info.Id;
                if (!toRemove && ForceRemove) {
                    toRemove = Confirm($"Confirm removing instance {file}, not matching file name");
                }

                if (toRemove) {
                    result.Add(file.Exists()
                        ? new KifaActionResult {
                            Status = KifaActionStatus.OK,
                            Message = $"File {file} deleted."
                        }
                        : new KifaActionResult {
                            Status = KifaActionStatus.Warning,
                            Message = $"File {file} not found."
                        });

                    file.Delete();

                    result.Add(FileInformation.Client.RemoveLocation(info.Id, location));
                }
            }
        }

        // Logical removal.
        result.Add(FileInformation.Client.Delete(info.Id));
        return result;
    }

    KifaActionResult RemoveFileInstance(KifaFile file) {
        var result = new KifaBatchActionResult();

        bool fileExists = file.Exists();
        if (!file.Registered) {
            if (RemoveLinkOnly) {
                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = "File not registered and only link is asked to be removed."
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
            result.Add(fileExists
                ? new KifaActionResult {
                    Status = KifaActionStatus.OK,
                    Message = $"File {file} deleted."
                }
                : new KifaActionResult {
                    Status = KifaActionStatus.Warning,
                    Message = $"File {file} not found."
                });
        }

        result.Add(FileInformation.Client.RemoveLocation(file.Id, file.ToString()));

        return result;
    }
}
