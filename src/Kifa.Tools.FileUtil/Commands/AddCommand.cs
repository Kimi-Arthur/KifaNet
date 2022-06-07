using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using NLog;
using Kifa.Api.Files;
using Kifa.IO;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("add", HelpText = "Add file entry.")]
class AddCommand : KifaCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('q', "quick", HelpText = "Check file in quick mode. Won't update registration.")]
    public bool QuickMode { get; set; } = false;

    [Option('f', "force", HelpText = "Check file integrity even if it is already registered.")]
    public bool ForceRecheck { get; set; } = false;

    public override int Execute() {
        var (multi, files) = KifaFile.ExpandFiles(FileNames);
        if (multi) {
            foreach (var file in files) {
                Console.WriteLine(file);
            }

            Console.Write($"Confirm adding the {files.Count} files above?");
            Console.ReadLine();
        }

        var executionHandler = new KifaExecutionHandler<KifaFile>(logger);

        foreach (var file in files) {
            executionHandler.Execute(new KifaFile(file.ToString()), AddFile, "Failed to add {0}.");
        }

        (_, files) = KifaFile.ExpandLogicalFiles(FileNames);
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

        return executionHandler.PrintSummary("Failed to add the following {0} files:");
    }

    void AddFile(KifaFile file) {
        logger.Info($"Adding {file}...");
        try {
            file.Add(QuickMode ? null : ForceRecheck);
            logger.Info($"Successfully added {file}.");
        } catch (IOException ex) {
            logger.Error(ex, $"Failed to add {file}.");
        }
    }
}
