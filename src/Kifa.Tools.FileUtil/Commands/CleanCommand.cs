using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Kifa.Api.Files;
using OpenQA.Selenium.DevTools;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("clean", HelpText = "Clean file entries.")]
class CleanCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('q', "quiet", HelpText = "Quiet mode, no confirmation is requested.")]
    public bool QuietMode { get; set; } = false;

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute() {
        RemoveMissingFiles();
        DeduplicateFiles();

        return 0;
    }

    void RemoveMissingFiles() {
        var files = KifaFile.FindPotentialFiles(FileNames);
        var filesToRemove = files.Where(file => file.HasEntry && !file.Exists()).ToList();

        if (filesToRemove.Count == 0) {
            Logger.Info("No missing files found.");
            return;
        }

        foreach (var file in filesToRemove) {
            Console.WriteLine(file);
        }

        Console.Write(
            $"The {filesToRemove.Count} files above do not actually exist. Confirm removing them from system?");
        Console.ReadLine();

        foreach (var file in filesToRemove) {
            file.Unregister();
        }
    }

    void DeduplicateFiles() {
        var files = KifaFile.FindExistingFiles(FileNames);
        if (files.Any(f => !f.IsLocal)) {
            Logger.Warn("The following files are not local. Aborted." + string.Join("\n\t",
                files.Where(f => !f.IsLocal)));
            // Error
            return;
        }

        foreach (var sameFiles in files.Where(f => f.FileInfo.Checked().Sha256 != null)
                     .GroupBy(f => $"{f.Host}/{f.FileInfo.Checked().Sha256}")) {
            var source = sameFiles.First();
            foreach (var target in sameFiles.Skip(1)) {
                if (!target.IsCompatible(source)) {
                    Logger.Warn(
                        $"File {target} is not in the same local cell as file {source}. Skipped.");
                    continue;
                }

                if (target.IsSameLocalFile(source)) {
                    Logger.Info($"File {target} is the same file as {source}. Skipped.");
                    continue;
                }

                if (!QuietMode && !Confirm($"Removing {target} and linking it to {source}...")) {
                    continue;
                }

                Logger.Info($"Removing {target} and linking it to {source}...");
                target.Delete();
                target.Unregister();
                source.Copy(target);

                // Skip the full check if the linking is from local file and in the same cell.
                // Caveat: It's only inferred that it used hard linking.
                target.Register(source.IsCompatible(target) && target.IsLocal);
                target.Add();
                Logger.Info($"Linked {target} to {source}.");
            }
        }
    }
}
