using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("add", HelpText = "Add file entry.")]
class AddCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('q', "quick", HelpText = "Check file in quick mode. Won't update registration.")]
    public bool QuickMode { get; set; } = false;

    [Option('f', "force", HelpText = "Check file integrity even if it is already registered.")]
    public bool ForceRecheck { get; set; } = false;

    [Option('m', "mirror-host",
        HelpText = "Mirror the file to the given host before checking if needed.")]
    public string? MirrorHost { get; set; }

    [Option('k', "keep-mirror", HelpText = "Keep the mirror version after checking.")]
    public bool KeepMirror { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var files = KifaFile.FindExistingFiles(FileNames);
        var selected = SelectMany(files, file => file.ToString(), "files to add");

        if (selected.Count == 0) {
            Logger.Warn("No files selected or found to be checked.");
            return 0;
        }

        foreach (var file in selected) {
            file.MirrorHost = MirrorHost;
            ExecuteItem(file.ToString(), () => AddFile(file));
            file.Dispose();
        }

        // TODO: This should be moved to somewhere.
        // Logger.Info("Looking for potential non-existing files to remove.");
        //
        // try {
        //     files = KifaFile.FindPotentialFiles(FileNames);
        // } catch (Exception ex) {
        //     Logger.Error(ex, "Failed to find potential non-existing files to remove.");
        //     return LogSummary();
        // }
        //
        // var filesToRemove = files.Where(file => file.HasEntry && !file.Registered && !file.Exists())
        //     .ToList();
        //
        // if (filesToRemove.Count > 0) {
        //     Console.Write(
        //         $"The following {filesToRemove.Count} files do not actually exist. Confirm removing them from system?");
        //     Console.ReadLine();
        //
        //     foreach (var file in filesToRemove) {
        //         file.Unregister();
        //     }
        // }

        return LogSummary();
    }

    void AddFile(KifaFile file) {
        Logger.Info($"Add {file}");
        file.Add(QuickMode ? null : ForceRecheck);
        if (MirrorHost != null && !KeepMirror) {
            file.RemoveLocalMirrorFile();
        }
    }
}
