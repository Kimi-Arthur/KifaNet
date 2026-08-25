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
            return 0;
        }

        var filesWithoutSha = infos.Where(f => f.Sha256 == null).DistinctBy(f => f.Id).ToList();
        foreach (var file in filesWithoutSha) {
            ExecuteItem(file.Id.Checked(),
                () => KifaActionResult.Skipped(
                    "No SHA256 calculated. Skipped duplicate info check."));
        }

        var filesWithSha = infos.Where(f => f.Sha256 != null).DistinctBy(f => f.Id).ToList();

        foreach (var sameFiles in filesWithSha.GroupBy(f => f.Sha256)) {
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

        var sha = fileList[0].Sha256;
        var confirmedKeep = SelectMany(fileList, f => f.Id.Checked(),
            $"info entries to keep for group {sha}");

        if (confirmedKeep.Status == KifaActionStatus.OK) {
            var keepIds = confirmedKeep.Value.Select(f => f.Id).ToHashSet();
            var filesToRemove = fileList.Where(f => !keepIds.Contains(f.Id)).ToList();

            ExecuteItem($"info entries for group {sha}", () => {
                var batch = new KifaBatchActionResult();

                foreach (var file in confirmedKeep.Value) {
                    batch.Add(file.Id.Checked(), KifaActionResult.Success("Kept info entry."));
                }

                foreach (var file in filesToRemove) {
                    batch.Add(file.Id.Checked(),
                        KifaFile.RemoveLogical(file.Id, force: true));
                }

                batch.Message = filesToRemove.Count > 0
                    ? $"Kept {confirmedKeep.Value.Count} info entry(ies), removed {filesToRemove.Count} duplicate(s)."
                    : $"Kept all {fileList.Count} info entries.";

                return batch;
            });
        } else {
            ExecuteItem($"info entries for group {sha}", () => confirmedKeep);
        }
    }
}
