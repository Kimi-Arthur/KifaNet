﻿using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("dedup", HelpText = "Deduplicate file entries.")]
class DedupCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('u', "Unsafe",
        HelpText =
            "Files to be deleted don't need to have a name sequence containing the base file.")]
    public bool Unsafe { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var files = KifaFile.FindPotentialFiles(FileNames).Select(f => f.FileInfo);
        var filesToDelete = new List<(string truth, FileInformation toDelete)>();
        foreach (var sameFiles in files.GroupBy(f => f.Sha256)) {
            var target = sameFiles.Select(f => (f.Id.Length, f.Id, f)).Min().f;
            foreach (var file in sameFiles) {
                if (file.Id != target.Id && (Unsafe || file.Id.ContainsSequence(target.Id))) {
                    filesToDelete.Add((target.Id, file));
                }
            }
        }

        if (filesToDelete.Count == 0) {
            Logger.Info("No duplicated files found!");
            return 0;
        }

        var confirmedDeletion = SelectMany(filesToDelete,
            tuple => $"{tuple.toDelete.Id} ({tuple.truth})", "files to delete");
        foreach (var file in confirmedDeletion.Select(d => d.toDelete)) {
            RemoveLogicalFile(file);
        }

        return 0;
    }

    void RemoveLogicalFile(FileInformation fileInfo) {
        var id = fileInfo.Id;
        foreach (var fileName in fileInfo.Locations.Keys) {
            var file = new KifaFile(fileName);
            if (file.Id == id) {
                Logger.Info($"Removing {file}...");
                file.Delete();
                Logger.Info($"Removing {file} from locations...");
                file.Unregister();
            }
        }

        Logger.Info($"Removing file info {fileInfo.Id}...");
        FileInformation.Client.Delete(fileInfo.Id);
        Logger.Info($"Successfully removed {fileInfo.Id}.");
    }
}
