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

    #region public late string DateString { get; set; }

    string? dateString;

    public string DateString {
        get => Late.Get(dateString);
        set => Late.Set(ref dateString, value);
    }

    #endregion

    public override int Execute(KifaTask? task = null) {
        var fileNames = FileNames.ToList();
        var foundFiles = KifaFile.FindAllFiles(fileNames);
        DateString = DateTime.UtcNow.ToString("yyyy-MM-dd_HH.mm.ss.ffffff");

        if (Restore) {
            var selectedFileIds = SelectMany(foundFiles, choiceName: "files to restore");
            Logger.Info("Should restore the files above.");
            return 1;
        } else {
            if (foundFiles.Count == 0) {
                Logger.Error("No files found.");
                return 1;
            }

            var selectedFiles = SelectMany(foundFiles, choiceName: "files to trash");
            var selectedFileIds = selectedFiles.Select(file => file.Id).ToHashSet();

            var extraFileIds = selectedFiles.SelectMany(f => f.FileInfo.Checked().GetAllLinks())
                .ToList();
            selectedFileIds.UnionWith(SelectMany(extraFileIds,
                choiceName: "extra versions of trashed files to trash"));

            var trashPath = Reason == null ? $"/Trash/{DateString}" : $"{DateString}_{Reason}";
            var entries = fileNames[0].Split("/", StringSplitOptions.RemoveEmptyEntries);
            if (entries.Length > 0) {
                trashPath += $"_{entries[^1]}";
            }

            selectedFileIds.ForEach(fileId => ExecuteItem(fileId, () => Trash(fileId, trashPath)));
            return LogSummary();
        }
    }

    KifaActionResult Trash(string file, string trashPath)
        => KifaActionResult.FromAction(() => {
            var client = FileInformation.Client;
            var target = $"{file}";
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
}
