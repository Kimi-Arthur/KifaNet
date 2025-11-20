using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("clean", HelpText = "Clean file entries.")]
class CleanCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute(KifaTask? task = null) {
        RemoveMissingFiles();
        DeduplicateFiles();

        return 0;
    }

    void RemoveMissingFiles() {
        var files = KifaFile.FindPotentialFiles(FileNames);
        var filesToRemove = files.Where(file => file.HasEntry && !file.Exists()).ToList();

        var selected = SelectMany(filesToRemove, f => f.ToString(), "non-existing files to remove");

        if (selected.Count == 0) {
            Logger.Info("No missing files found or selected.");
            return;
        }

        foreach (var file in selected) {
            file.Unregister();
        }
    }

    void DeduplicateFiles() {
        var files = KifaFile.FindPotentialFiles(FileNames);
        foreach (var file in files) {
            var info = file.FileInfo.Checked();
            info.Id = null;
            var sameHostFiles = info.Locations
                .Where(f => f.Value != null && new FileLocation(f.Key).Server == file.Host)
                .Select(f => new KifaFile(f.Key, fileInfo: info)).ToList();
            var filesById = sameHostFiles.GroupBy(f => f.FileId).ToList();
            if (filesById.Count == 1) {
                Logger.Info(
                    $"No need to dedup these files:\n\t{string.Join("\n\t", sameHostFiles.Select(f => $"{f} ({f.FileId}"))}");
                continue;
            }

            var selected = SelectOne(filesById,
                group
                    => $"{group.Key} ({group.Count()} refs, {group.First().GetRefCount()} refs in OS):\n{group.Select(f => f.ToString()).JoinBy("\n")})",
                "file to keep");

            if (selected == null) {
                Logger.Error("No proper file is selected");
                continue;
            }

            foreach (var group in filesById) {
                if (group == selected.Value.Choice) {
                    continue;
                }

                foreach (var f in group) {
                    f.Delete();
                    f.Unregister();
                    selected.Value.Choice.First().Copy(f);
                    f.Add();
                    Logger.Info($"Removed and relinked {f} ({f.FileId}).");
                }
            }
        }
    }
}
