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
        "Make the files the only conceptual items by removing duplicate info entries and instances.")]
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
            Logger.Info("No files found.");
            return 0;
        }

        var filesWithSha = infos.Where(f => !string.IsNullOrEmpty(f.Sha256)).DistinctBy(f => f.Id)
            .ToList();

        var infoEntriesToRemove = new List<(FileInformation target, FileInformation toDelete)>();

        foreach (var sameFiles in filesWithSha.GroupBy(f => f.Sha256)) {
            var fileList = sameFiles.ToList();
            if (fileList.Count <= 1) {
                continue;
            }

            var target = fileList.OrderBy(f => f.Id.Checked().Length).ThenBy(f => f.Id).First();
            foreach (var file in fileList) {
                if (file.Id != target.Id) {
                    infoEntriesToRemove.Add((target, file));
                }
            }
        }

        if (infoEntriesToRemove.Count > 0) {
            var confirmedInfoEntries = SelectMany(infoEntriesToRemove,
                tuple => $"{tuple.toDelete.Id} (target: {tuple.target.Id})",
                "info entries to remove");

            if (confirmedInfoEntries.Status == KifaActionStatus.OK) {
                foreach (var tuple in confirmedInfoEntries.Value) {
                    ExecuteItem(tuple.toDelete.Id.Checked(),
                        () => KifaFile.RemoveLogical(tuple.toDelete.Id, force: true));
                }
            } else {
                ExecuteItem("info entries to remove", () => confirmedInfoEntries);
            }
        } else {
            Logger.Info("No duplicate info entries found.");
        }

        var activeInfoIds = infos.Select(f => f.Id).Distinct().ToList();
        var activeInfos = activeInfoIds.Select(id => FileInformation.Client.Get(id))
            .Where(info => info != null).Cast<FileInformation>().ToList();

        var instancesToRemove = new List<(FileInformation info, string location)>();

        foreach (var info in activeInfos) {
            if (info.Locations.Count <= 1) {
                continue;
            }

            var primaryLocation = info.Locations.Keys
                .OrderByDescending(loc => new KifaFile(loc).Id == info.Id)
                .ThenByDescending(loc => new KifaFile(loc).IsLocal).ThenBy(loc => loc.Length)
                .First();

            foreach (var location in info.Locations.Keys) {
                if (location != primaryLocation) {
                    instancesToRemove.Add((info, location));
                }
            }
        }

        if (instancesToRemove.Count > 0) {
            var confirmedInstances = SelectMany(instancesToRemove,
                tuple => $"{tuple.location} (for {tuple.info.Id})", "file instances to remove");

            if (confirmedInstances.Status == KifaActionStatus.OK) {
                foreach (var tuple in confirmedInstances.Value) {
                    ExecuteItem($"{tuple.location} ({tuple.info.Id})",
                        () => new KifaFile(tuple.location).RemoveInstance());
                }
            } else {
                ExecuteItem("file instances to remove", () => confirmedInstances);
            }
        } else {
            Logger.Info("No duplicate file instances found.");
        }

        return LogSummary();
    }
}
