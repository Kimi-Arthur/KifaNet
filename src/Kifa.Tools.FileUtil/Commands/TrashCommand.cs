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

    [Option('w', "reason",
        HelpText =
            "Attach reason to the trashed files after the top datetime folder. No space should be here.")]
    public virtual string? Reason { get; set; }

    [Option('r', "restore", HelpText = "Restore trashed files")]
    public bool Restore { get; set; } = false;

    public override int Execute() {
        var foundFiles = KifaFile.FindAllFiles(FileNames);
        var fileIds = foundFiles.Select(f => f.Id).ToList();

        if (Restore) {
            var selectedFileIds = SelectMany(fileIds, choiceName: "files to restore");
            Logger.Info("Should restore the files above.");
            return 1;
        } else {
            if (fileIds.Count == 0) {
                Logger.Error("No files found.");
                return 1;
            }

            var selectedFileIds = SelectMany(fileIds, choiceName: "files to trash");

            var extraFileIds = foundFiles.SelectMany(f => f.FileInfo.Checked().GetOtherLinks())
                .ToList();
            if (extraFileIds.Count > 0) {
                selectedFileIds.AddRange(SelectMany(extraFileIds,
                    choiceName: "extra versions of trashed files to trash"));
            }

            selectedFileIds.ForEach(fileId => ExecuteItem(fileId, () => Trash(fileId)));
            return LogSummary();
        }
    }

    KifaActionResult Trash(string file)
        => KifaActionResult.FromAction(() => {
            var client = FileInformation.Client;
            var target = $"/Trash/{GetReasonPath()}{file}";
            client.Link(file, target);
            Logger.Info($"Linked original FileInfo {file} to new FileInfo {target}.");

            var targetInfo = client.Get(target).Checked();
            foreach (var location in targetInfo.Locations.Keys) {
                var instance = new KifaFile(location);
                if (instance.Id == file) {
                    if (instance.IsLocal && instance.Exists()) {
                        var targetInstance = new KifaFile(instance.Host + targetInfo.Id);
                        instance.Move(targetInstance);
                        Logger.Info($"File {instance} moved to {targetInstance}.");
                        targetInstance.Register(true);
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

    string GetReasonPath() {
        var dateString = DateTime.UtcNow.ToString("yyyy-MM-dd_HH.mm.ss.ffffff");
        return Reason == null ? dateString : $"{dateString}_{Reason}";
    }
}
