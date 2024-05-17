using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using NLog;
using OpenQA.Selenium.DevTools;

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

    public override int Execute() {
        var files = KifaFile.FindExistingFiles(FileNames);
        foreach (var file in files) {
            Console.WriteLine(file);
        }

        Console.Write($"Confirm adding the {files.Count} files above?");
        Console.ReadLine();

        foreach (var file in files) {
            ExecuteItem(file.ToString(), () => AddFile(new KifaFile(file.ToString())));
            file.Dispose();
        }

        Logger.Debug("Looking for potential non-existing files to remove.");

        try {
            files = KifaFile.FindPotentialFiles(FileNames);
        } catch (Exception ex) {
            Logger.Error(ex, "Failed to find potential non-existing files to remove.");
            return LogSummary();
        }

        var filesToRemove = files.Where(file => file.HasEntry && !file.Registered && !file.Exists())
            .ToList();

        if (filesToRemove.Count > 0) {
            Console.Write(
                $"The following {filesToRemove.Count} files do not actually exist. Confirm removing them from system?");
            Console.ReadLine();

            foreach (var file in filesToRemove) {
                file.Unregister();
            }
        }

        return LogSummary();
    }

    void AddFile(KifaFile file) {
        Logger.Info($"Adding {file}...");
        file.Add(QuickMode ? null : ForceRecheck);
    }
}
