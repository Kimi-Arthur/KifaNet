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

[Verb("uniq",
    HelpText =
        "Make the files the only conceptual items by removing duplicate info entries within the given list.")]
class UniqCommand : KifaFileCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to process.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('i', "id", HelpText = "Treat input files as logical ids.")]
    public bool ById { get; set; } = false;

    [Option('S', "show-size", HelpText = "Show size for each file and total size.")]
    public bool ShowSize { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        if (!ById) {
            var localFiles = KifaFile.FindExistingFiles(FileNames);
            RegisterUnregisteredFiles(localFiles, ShowSize, "making unique");
        }

        var infos = FindFileInfos(FileNames, ById);
        if (infos.Count == 0) {
            Logger.Warn("No files found.");
            return 1;
        }

        var selected = SelectMany(infos,
            info => ShowSize ? $"{info.Id} ({info.Size.ToSizeString()})" : info.Id.Checked(),
            new Func<List<FileInformation>, string>(choices
                => $"files{(ShowSize ? $" ({choices.Sum(c => c.Size ?? 0).ToSizeString()})" : "")} to make unique"));

        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("files to make unique", () => selected);
            return LogSummary();
        }

        infos = selected.Value;

        var filesWithoutSha = infos.Where(f => f.Sha256 == null).ToList();
        if (filesWithoutSha.Count > 0) {
            foreach (var file in filesWithoutSha) {
                ExecuteItem(file.Id.Checked(),
                    () => KifaActionResult.Error("No SHA256 calculated."));
            }

            return LogSummary();
        }

        foreach (var sameFiles in infos.GroupBy(f => f.Sha256)) {
            DeduplicateGroup(sameFiles.ToList());
        }

        return LogSummary();
    }

    void DeduplicateGroup(List<FileInformation> fileList) {
        if (fileList.Count <= 1) {
            if (fileList.Count == 1) {
                ExecuteItem(fileList[0].Id.Checked(),
                    () => KifaActionResult.Success("No duplicate info entries."));
            }

            return;
        }

        var sha = fileList[0].Sha256.Checked();

        var defaultTargets = (UploadCommand.DefaultTargets ?? []).Select(CloudTarget.Parse).ToList();
        var allLocations = fileList.SelectMany(f => f.Locations)
            .Where(kv => kv.Value != null).Select(kv => kv.Key).ToHashSet();

        if (defaultTargets.Count > 0) {
            var missingTargets = defaultTargets.Where(target
                => !allLocations.Any(l => l.StartsWith($"{target.ServiceType.ToString().ToLower()}:") &&
                                          l.EndsWith($"/$/{sha}.{target.FormatType}"))).ToList();
            if (missingTargets.Count > 0) {
                ExecuteItem($"info entries for group {sha}",
                    () => KifaActionResult.Error(
                        $"File is not fully uploaded to cloud targets. Missing: {missingTargets.Select(t => t.ToString()).JoinBy(", ")}."));
                return;
            }
        } else {
            var hasCloudInstance = allLocations.Any(l => l.Contains($"/$/{sha}."));
            if (!hasCloudInstance) {
                ExecuteItem($"info entries for group {sha}",
                    () => KifaActionResult.Error("No cloud instances found."));
                return;
            }
        }

        var confirmedKeep = SelectMany(fileList, f => f.Id.Checked(),
            $"info entries to keep for group {sha}");

        if (confirmedKeep.Status != KifaActionStatus.OK) {
            ExecuteItem($"info entries for group {sha}", () => confirmedKeep);
            return;
        }

        var keepIds = confirmedKeep.Value.Select(f => f.Id).ToHashSet();
        if (keepIds.Count == 0) {
            ExecuteItem($"info entries for group {sha}",
                () => KifaActionResult.Error("No info entries selected to keep."));
            return;
        }

        var filesToRemove = fileList.Where(f => !keepIds.Contains(f.Id)).ToList();

        ExecuteItem($"info entries for group {sha}", () => {
            var result = new KifaBatchActionResult();

            foreach (var file in confirmedKeep.Value) {
                result.Add(file.Id.Checked(), KifaActionResult.Success("Kept info entry."));
            }

            foreach (var file in filesToRemove) {
                result.Add(file.Id.Checked(), KifaFile.RemoveLogical(file.Id, force: true));
            }

            result.Message = filesToRemove.Count > 0
                ? $"Kept {confirmedKeep.Value.Count} info entry(ies), removed {filesToRemove.Count} duplicate(s)."
                : $"Kept all {fileList.Count} info entries.";

            return result;
        });
    }
}
