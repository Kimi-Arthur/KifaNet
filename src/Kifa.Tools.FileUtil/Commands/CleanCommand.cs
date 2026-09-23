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

[Verb("clean", HelpText = "Clean file entries.")]
public class CleanCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('S', "show-size", HelpText = "Show size for each file and total size (can be slow).")]
    public bool ShowSize { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        RemoveMissingFiles();
        DeduplicateFiles();

        return LogSummary();
    }

    void RemoveMissingFiles() {
        var files = KifaFile.FindPotentialFiles(FileNames);
        var filesToRemove = files.Where(file => file.HasEntry && !file.Exists()).ToList();

        var selected = SelectMany(filesToRemove,
            f => ShowSize && f.FileInfo?.Size != null
                ? $"{f} ({f.FileInfo.Size.ToSizeString()})"
                : f.ToString(),
            new Func<List<KifaFile>, string>(choices
                => $"non-existing files{(ShowSize ? $" ({choices.Sum(c => c.FileInfo?.Size ?? 0).ToSizeString()})" : "")} to remove"));

        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("non-existing files to remove", () => selected);
            return;
        }

        foreach (var file in selected.Value) {
            ExecuteItem(file.ToString(), () => file.Unregister());
        }
    }

    void DeduplicateFiles() {
        var files = KifaFile.FindPotentialFiles(FileNames);
        var processed = new HashSet<string>();
        foreach (var file in files) {
            var info = file.FileInfo;
            if (info == null) {
                continue;
            }

            var groupKey = $"{file.Host}:{info.RealId ?? info.Id ?? info.Sha256}";
            if (!processed.Add(groupKey)) {
                continue;
            }

            var sameHostFiles = info.Locations
                .Where(f => f.Value != null && new FileLocation(f.Key).Server == file.Host)
                .Select(f => new KifaFile(f.Key, fileInfo: info))
                .Where(f => f.Exists())
                .ToList();

            var filesById = sameHostFiles.GroupBy(f => f.FileId).ToList();
            if (filesById.Count <= 1) {
                Logger.Info(
                    $"No need to dedup these files:\n\t{string.Join("\n\t", sameHostFiles.Select(f => $"{f} ({f.FileId})"))}");
                continue;
            }

            ExecuteItem($"Deduplicate {file}", () => DedupFileGroup(filesById));
        }
    }

    public static int GetOutsideLinks(IGrouping<string?, KifaFile> group) {
        var refCount = group.First().GetRefCount();
        if (refCount == null) {
            return 0;
        }

        return Math.Max(0, Convert.ToInt32(refCount.Value) - group.Count());
    }

    public static bool TestCanLink(KifaFile source, KifaFile destination) {
        try {
            if (!source.Exists()) {
                return false;
            }

            var tempFile = destination.Parent.GetFile($".kifa_test_link_{Guid.NewGuid():N}");
            try {
                source.Copy(tempFile);
                tempFile.Delete();
                return true;
            } catch (Exception ex) {
                Logger.Debug(ex, $"Test link from {source} to {destination} failed.");
                try {
                    if (tempFile.Exists()) {
                        tempFile.Delete();
                    }
                } catch {
                    // Ignore cleanup error.
                }

                return false;
            }
        } catch (Exception ex) {
            Logger.Debug(ex, $"Test link setup failed for {destination}.");
            return false;
        }
    }

    public static bool CanGroupReplaceAllOthers(IGrouping<string?, KifaFile> candidateGroup,
        List<IGrouping<string?, KifaFile>> allGroups) {
        var source = candidateGroup.First();
        foreach (var otherGroup in allGroups) {
            if (otherGroup == candidateGroup) {
                continue;
            }

            foreach (var file in otherGroup) {
                if (!TestCanLink(source, file)) {
                    return false;
                }
            }
        }

        return true;
    }

    public KifaActionResult DedupFileGroup(List<IGrouping<string?, KifaFile>> filesById) {
        var validCandidates = filesById.Where(g => CanGroupReplaceAllOthers(g, filesById)).ToList();
        if (validCandidates.Count == 0) {
            return KifaActionResult.Error(
                "Cannot deduplicate files: permission denied or unable to create links in target folders.");
        }

        var orderedCandidates = validCandidates
            .OrderByDescending(GetOutsideLinks)
            .ThenByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .ToList();

        var hasOutsideLinks = filesById.Any(g => GetOutsideLinks(g) > 0);
        IGrouping<string?, KifaFile> chosenGroup;

        if (hasOutsideLinks) {
            var selected = SelectOne(orderedCandidates,
                group => $"{group.Key} ({group.Count()} refs, {GetOutsideLinks(group)} more refs in OS):\n" +
                         $"{group.Select(f => $"\t{f}").JoinBy("\n")}",
                "file to keep");

            if (selected.Status != KifaActionStatus.OK) {
                return selected;
            }

            chosenGroup = selected.Value.Choice;
        } else {
            chosenGroup = orderedCandidates[0];
        }

        var targetFile = chosenGroup.First();

        foreach (var group in filesById) {
            if (group == chosenGroup) {
                continue;
            }

            foreach (var f in group) {
                if (f.IsSameLocalFile(targetFile)) {
                    continue;
                }

                var tempFile = f.Parent.GetFile($".kifa_dedup_{Guid.NewGuid():N}_{f.Name}");
                try {
                    targetFile.Copy(tempFile);
                } catch (Exception ex) {
                    Logger.Error(ex, $"Failed to create link from {targetFile} to {tempFile}.");
                    return KifaActionResult.Error($"Failed to link {targetFile} to {f}: {ex.Message}");
                }

                try {
                    f.Delete();
                    tempFile.Move(f);
                } catch (Exception ex) {
                    Logger.Error(ex, $"Failed to replace {f} with {tempFile}.");
                    try {
                        if (tempFile.Exists()) {
                            tempFile.Delete();
                        }
                    } catch {
                        // Ignore cleanup error.
                    }

                    return KifaActionResult.Error($"Failed to replace {f}: {ex.Message}");
                }

                f.Unregister();
                var relinkedFile = new KifaFile(f.ToString());
                relinkedFile.Add();
                Logger.Info($"Removed and relinked {relinkedFile} ({relinkedFile.FileId}).");
            }
        }

        return KifaActionResult.Success();
    }
}
