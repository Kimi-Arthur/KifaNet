using System;
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

    [Value(0, MetaName = "FILE", MetaValue = "STRING", HelpText = "File to be removed.")]
    public string FileUri { get; set; }

    [Option('i', "id", HelpText = "ID for the uri.")]
    public string FileId { get; set; }

    [Option('l', "link", HelpText = "Remove link only.")]
    public bool RemoveLinkOnly { get; set; }

    [Option('f', "force",
        HelpText =
            "Remove all instances of the file, including file with different name and in cloud.")]
    public bool ForceRemove { get; set; }

    public override int Execute() {
        if (string.IsNullOrEmpty(FileUri)) {
            var files = FileInformation.Client.ListFolder(FileId, true);
            if (files.Count > 0) {
                foreach (var file in files) {
                    Console.WriteLine(file);
                }

                var removalText = RemoveLinkOnly ? "" : " and remove them from file system";
                Console.Write($"Confirm deleting the {files.Count} files above{removalText}?");
                Console.ReadLine();

                return files.Select(f => RemoveLogicalFile(FileInformation.Client.Get(f))).Max();
            }

            return RemoveLogicalFile(FileInformation.Client.Get(FileId));
        }

        var source = new KifaFile(FileUri);

        // TODO: testing whether it's folder or not with Exists is not optimal.
        if (!source.Exists()) {
            var localFiles = source.List(true).ToList();
            foreach (var file in localFiles) {
                Console.WriteLine(file);
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

            var removalText = RemoveLinkOnly ? "" : " and remove them from file system";
            Console.Write($"Confirm deleting the {localFiles.Count} files above{removalText}?");
            Console.ReadLine();

            return localFiles.Concat(potentialFileInstances)
                .Select(f => RemoveFileInstance(new KifaFile(f.ToString()))).Max();
        }

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
