using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("trash", HelpText = "Move the file to trash.")]
class TrashCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target files to trash.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute() {
        var foundFiles = KifaFile.FindAllFiles(FileNames);
        var fileIds = foundFiles.Select(f => f.Id).ToList();
        foreach (var fileId in fileIds) {
            Console.WriteLine(fileId);
        }

        var trashFolder =
            Confirm($"Confirm trashing the {fileIds.Count} files above to the .Trash/ folder in:",
                fileIds[0][..fileIds[0].LastIndexOf('/')]);

        var fileIdsByResult = fileIds.Select(fileId => (fileId, result: Trash(fileId, trashFolder)))
            .GroupBy(item => item.result.Status == KifaActionStatus.OK)
            .ToDictionary(item => item.Key, item => item.ToList());
        if (fileIdsByResult.ContainsKey(true)) {
            var files = fileIdsByResult[true];
            Logger.Info($"Successfully trashed the following {files.Count} files:");
            foreach (var (file, _) in files) {
                Logger.Info($"\t{file}");
            }
        }

        if (fileIdsByResult.ContainsKey(false)) {
            var files = fileIdsByResult[false];
            Logger.Info($"Failed to trash the following {files.Count} files:");
            foreach (var (file, result) in files) {
                Logger.Info($"\t{file}: {result.Message}");
            }

            return 1;
        }

        return 0;
    }

    static KifaActionResult Trash(string file, string trashFolder)
        => KifaActionResult.FromAction(() => {
            var client = FileInformation.Client;
            var target = trashFolder + "/.Trash" + file[trashFolder.Length..];
            client.Link(file, target);
            Logger.Info($"Linked original FileInfo {file} to new FileInfo {target}.");

            var targetInfo = client.Get(target);
            foreach (var location in targetInfo.Locations.Keys) {
                var instance = new KifaFile(location);
                if (instance.Id == file) {
                    if (instance.Exists()) {
                        var targetInstance = new KifaFile(instance.Host + targetInfo.Id);
                        instance.Move(targetInstance);
                        Logger.Info($"File {instance} moved to {targetInstance}.");
                        targetInstance.Add();
                    } else {
                        Logger.Warn($"File {instance} not found.");
                    }

                    client.RemoveLocation(targetInfo.Id, location);
                    Logger.Info($"Entry {location} removed.");
                }
            }

            client.Delete(file);
            Logger.Info($"Original FileInfo {file} removed.");
        });
}
