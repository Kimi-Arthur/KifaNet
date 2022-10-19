using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
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
            }

            Logger.Fatal("No files found!");
            return 1;
        }

        var (_, localFiles) = KifaFile.FindExistingFiles(FileNames);

            foreach (var file in localFiles) {
                Console.WriteLine(file);
            }

            if (Confirm($"Confirm deleting the {localFiles.Count} files above{removalText}?")) {
                localFiles.ForEach(f
                    => ExecuteItem(f, () => RemoveLogicalFile(FileInformation.Client.Get(f)!)));
                return LogSummary();
            }

            var potentialFiles =
                FileInformation.Client.Get(FileInformation.Client.ListFolder(source.Id, true));
            if (potentialFiles.Count == 0) {
                potentialFiles.Add(FileInformation.Client.Get(source.Id));
            }

            var potentialFileInstances = potentialFiles
                .Select(f => f.Locations.Keys.Select(l => new KifaFile(l))
                    .FirstOrDefault(l => l.Host == source.Host))
                .Where(f => f != null && !localFiles.Contains(f)).ToList();

            if (potentialFileInstances.Any()) {
                foreach (var file in potentialFileInstances) {
                    Console.WriteLine($"{file} (link only)");
                }
            }

            Console.Write($"Confirm deleting the {localFiles.Count} files above{removalText}?");
            Console.ReadLine();

            return localFiles.Concat(potentialFileInstances)
                .Select(f => RemoveFileInstance(new KifaFile(f.ToString()))).Max();

        return RemoveFileInstance(source);
    }

    int RemoveLogicalFile(FileInformation info) {
        if (!RemoveLinkOnly && info.Locations != null) {
            foreach (var location in info.Locations.Keys) {
                var file = new KifaFile(location);

                var toRemove = file.Id == info.Id;
                if (!toRemove && ForceRemove) {
                    toRemove = Confirm($"Confirm removing instance {file}, not matching file name");
                }

                if (toRemove) {
                    if (file.Exists()) {
                        file.Delete();
                        Logger.Info($"File {file} deleted.");
                    } else {
                        Logger.Warn($"File {file} not found.");
                    }

                    FileInformation.Client.RemoveLocation(info.Id, location);
                    Logger.Info($"Entry {location} removed.");
                }
            }
        }

        // Logical removal.
        FileInformation.Client.Delete(info.Id);
        Logger.Info($"FileInfo {info.Id} removed.");
        return 0;
    }

    int RemoveFileInstance(KifaFile file) {
        if (file.FileInfo?.Locations?.ContainsKey(file.ToString()) != true) {
            if (file.Exists()) {
                file.Delete();
                Logger.Warn($"File {file} deleted, no entry found though.");
            } else {
                file.Delete();
                Logger.Warn($"File {file} not found.");
            }

            return 0;
        }

        // Remove specific location item.
        if (!RemoveLinkOnly) {
            if (file.Exists()) {
                file.Delete();
                Logger.Info($"File {file} deleted.");
            } else {
                file.Delete();
                Logger.Warn($"File {file} not found.");
            }
        }

        FileInformation.Client.RemoveLocation(file.Id, file.ToString());
        Logger.Info($"Entry {file} removed.");

        return 0;
    }
}
