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
class CleanCommand : KifaCommand {
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
        // TODO: Should probably group by Id first.
        foreach (var file in files) {
            var info = file.FileInfo.Checked();
            info.Id = null;
            var sameHostFiles = info.Locations
                .Where(f => f.Value != null && new FileLocation(f.Key).Server == file.Host)
                .Select(f => new KifaFile(f.Key, fileInfo: info)).ToList();
            var filesById = sameHostFiles.GroupBy(f => f.FileId).ToList();
            if (filesById.Count == 1) {
                Logger.Info(
                    $"No need to dedup these files:\n\t{string.Join("\n\t", sameHostFiles.Select(f => $"{f} ({f.FileId})"))}");
                continue;
            }

            ExecuteItem($"Deduplicate {file}", () => DedupFileGroup(filesById));
        }
    }

    KifaActionResult DedupFileGroup(List<IGrouping<string?, KifaFile>> filesById) {
        var selected = SelectOne(filesById,
            group
                => $"{group.Key} ({group.Count()} refs, {Convert.ToInt32(group.First().GetRefCount()) - group.Count()} more refs in OS):\n" +
                   $"{group.Select(f => $"\t{f}").JoinBy("\n")}", "file to keep");

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

        return KifaActionResult.Success();
    }
}
