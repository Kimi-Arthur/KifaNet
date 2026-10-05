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
public class UniqCommand : KifaFileCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to process.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('i', "id", HelpText = "Treat input files as logical ids.")]
    public bool ById { get; set; } = false;

    [Option('a', "include-all", HelpText = "Include all files already registered or ignored.")]
    public bool IncludeAll { get; set; } = false;

    [Option('p', "preferred-folder", HelpText = "Preferred folder to keep instances in.")]
    public string? PreferredFolder { get; set; }

    [Option('u', "unuploaded",
        HelpText = "Allow making unique without requiring files to be uploaded to cloud.")]
    public bool Unuploaded { get; set; } = false;

    [Option('S', "show-size", HelpText = "Show size for each file and total size.")]
    public bool ShowSize { get; set; } = false;

    [Option('x', "cross-only",
        HelpText = "Only process duplicate files that appear across different input groups / preferred folder.")]
    public bool CrossOnly { get; set; } = false;

    public static string? GetDefaultKeepReply(List<FileInformation> fileList,
        string? preferredFolderId) {
        if (preferredFolderId != null) {
            var folderPrefix = $"{preferredFolderId}/";
            var matchingIndexes = new List<int>();
            for (var i = 0; i < fileList.Count; i++) {
                var fileId = fileList[i].Id.Checked();
                if (fileId == preferredFolderId || fileId.StartsWith(folderPrefix)) {
                    matchingIndexes.Add(i + 1);
                }
            }

            if (matchingIndexes.Count > 0) {
                return string.Join(",", matchingIndexes);
            }
        }

        if (fileList.Count < 2) {
            return null;
        }

        for (var i = 0; i < fileList.Count; i++) {
            var candidateId = fileList[i].Id.Checked();
            var isWinner = true;
            for (var j = 0; j < fileList.Count; j++) {
                if (i == j) {
                    continue;
                }

                if (!IsSubsequence(candidateId, fileList[j].Id.Checked())) {
                    isWinner = false;
                    break;
                }
            }

            if (isWinner) {
                return (i + 1).ToString();
            }
        }

        return null;
    }

    static bool IsSubsequence(string shorter, string longer) {
        if (shorter.Length >= longer.Length) {
            return false;
        }

        var i = 0;
        var j = 0;
        while (i < shorter.Length && j < longer.Length) {
            if (shorter[i] == longer[j]) {
                i++;
            }

            j++;
        }

        return i == shorter.Length;
    }

    static bool IsInFolder(FileInformation file, string? folderId) {
        if (folderId == null) {
            return false;
        }

        var fileId = file.Id.Checked();
        return fileId == folderId || fileId.StartsWith($"{folderId}/");
    }

    public override int Execute(KifaTask? task = null) {
        var fileNames = FileNames.ToList();
        var preferredFolder = PreferredFolder ?? (fileNames.Count >= 2 ? fileNames[0] : null);
        var preferredFolderId =
            preferredFolder != null ? GetLogicalId(preferredFolder, ById) : null;

        if (!ById) {
            var localFiles = KifaFile.FindExistingFiles(fileNames, ignoreFiles: !IncludeAll);
            RegisterUnregisteredFiles(localFiles, ShowSize, "making unique");
        }

        var infos = FindFileInfos(fileNames, ById, ignoreFiles: !IncludeAll);
        if (infos.Count == 0) {
            Logger.Warn("No files found.");
            return 1;
        }

        var filesWithoutSha = infos.Where(f => f.Sha256 == null).ToList();
        if (filesWithoutSha.Count > 0) {
            foreach (var file in filesWithoutSha) {
                ExecuteItem(file.Id.Checked(),
                    () => KifaActionResult.Error("No SHA256 calculated."));
            }

            return LogSummary();
        }

        if (CrossOnly) {
            if (preferredFolderId == null) {
                Logger.Error("Cross-only mode requires a preferred folder or at least two folders.");
                return 1;
            }

            var allGroups = infos.GroupBy(f => f.Sha256.Checked()).ToList();
            var activeInfos = new List<FileInformation>();
            var ignoredInfos = new List<FileInformation>();

            foreach (var group in allGroups) {
                var groupFiles = group.ToList();
                var isCrossGroup = groupFiles.Any(f => IsInFolder(f, preferredFolderId)) &&
                                   groupFiles.Any(f => !IsInFolder(f, preferredFolderId));
                if (isCrossGroup) {
                    activeInfos.AddRange(groupFiles);
                } else {
                    ignoredInfos.AddRange(groupFiles);
                }
            }

            if (ignoredInfos.Count > 0) {
                Console.WriteLine(
                    $"The following {ignoredInfos.Count} file(s){(ShowSize ? $" ({ignoredInfos.Sum(f => f.Size ?? 0).ToSizeString()})" : "")} will NOT be processed (not cross-group duplicates):");
                foreach (var file in ignoredInfos) {
                    Console.WriteLine(ShowSize
                        ? $"  {file.Id.Checked()} ({file.Size.ToSizeString()})"
                        : $"  {file.Id.Checked()}");
                }

                Console.WriteLine();
            }

            if (activeInfos.Count == 0) {
                Logger.Info("No duplicate files found across groups.");
                return 0;
            }

            infos = activeInfos;
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

        foreach (var sameFiles in infos.GroupBy(f => f.Sha256)) {
            var fileList = sameFiles.ToList();
            var sha = fileList[0].Sha256.Checked();
            ExecuteItem(fileList.Count == 1 ? fileList[0].Id.Checked() : $"info entries for group {sha}",
                () => DeduplicateGroup(fileList, preferredFolderId));
        }

        return LogSummary();
    }

    public KifaActionResult DeduplicateGroup(List<FileInformation> fileList,
        string? preferredFolderId = null) {
        if (fileList.Count == 0) {
            return KifaActionResult.Skipped("No file entries.");
        }

        var sha = fileList[0].Sha256.Checked();

        var checkResult = CheckCloud(fileList);
        if (checkResult.Status != KifaActionStatus.OK) {
            return checkResult;
        }

        if (fileList.Count == 1) {
            return KifaActionResult.Skipped("No duplicate info entries.");
        }

        var defaultReply = GetDefaultKeepReply(fileList, preferredFolderId);

        var confirmedKeep = SelectMany(fileList, f => f.Id.Checked(),
            $"info entries to keep for group {sha}", defaultReply: defaultReply);

        if (confirmedKeep.Status != KifaActionStatus.OK) {
            return confirmedKeep;
        }

        var keepIds = confirmedKeep.Value.Select(f => f.Id).ToHashSet();
        if (keepIds.Count == 0) {
            return KifaActionResult.Error("No info entries selected to keep.");
        }

        var filesToRemove = fileList.Where(f => !keepIds.Contains(f.Id)).ToList();
        if (filesToRemove.Count == 0) {
            return KifaActionResult.Skipped($"Kept all {fileList.Count} info entries.");
        }

        var result = new KifaBatchActionResult();

        foreach (var file in confirmedKeep.Value) {
            result.Add(file.Id.Checked(), KifaActionResult.Skipped("Kept info entry."));
        }

        foreach (var file in filesToRemove) {
            result.Add(file.Id.Checked(), KifaFile.RemoveLogical(file.Id, force: true));
        }

        result.Message =
            $"Kept {confirmedKeep.Value.Count} info entries, removed {filesToRemove.Count} duplicates.";

        return result;
    }

    KifaActionResult CheckCloud(List<FileInformation> fileList) {
        if (Unuploaded) {
            var missingFiles = fileList.Where(f => !f.ExistsSomewhere()).ToList();
            if (missingFiles.Count > 0) {
                return KifaActionResult.Error(
                    $"Files not found in any location: {missingFiles.Select(f => f.Id).JoinBy(", ")}.");
            }

            return KifaActionResult.Success();
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
                return KifaActionResult.Error(
                    $"File is not fully uploaded to cloud targets. Missing: {missingTargets.Select(t => t.ToString()).JoinBy(", ")}.");
            }
        } else {
            var hasCloudInstance = allLocations.Any(l => l.Contains($"/$/{sha}."));
            if (!hasCloudInstance) {
                return KifaActionResult.Error("No cloud instances found.");
            }
        }

        Logger.Info($"Group {sha} is fully uploaded to cloud.");
        return KifaActionResult.Success();
    }
}
