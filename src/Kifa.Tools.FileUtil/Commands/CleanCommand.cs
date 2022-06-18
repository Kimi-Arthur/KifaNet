using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Kifa.Api.Files;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("clean", HelpText = "Clean file entries.")]
class CleanCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute() {
        RemoveMissingFiles();
        DeduplicateFiles();

        return 0;
    }

    void RemoveMissingFiles() {
        var (_, files) = KifaFile.ExpandLogicalFiles(FileNames, fullFile: true);
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
        var (_, files) = KifaFile.ExpandFiles(FileNames, fullFile: true);
        foreach (var sameFiles in files.GroupBy(f => $"{f.Host}/{f.FileInfo.Sha256}")) {
            var target = sameFiles.First();
            foreach (var file in sameFiles.Skip(1)) {
                Logger.Info($"Removing {file} and linking it to {target}...");
                file.Delete();
                file.Unregister();
                target.Copy(file);
                file.Add();
                Logger.Info($"Linked {file} to {target}.");
            }
        }
    }
}
