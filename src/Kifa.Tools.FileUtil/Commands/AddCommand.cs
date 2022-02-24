using System;
using System.Collections.Generic;
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

    [Option('f', "force-check", HelpText = "Check file integrity even if it is already recorded.")]
    public bool ForceRecheck { get; set; } = false;

    [Option('o', "overwrite", HelpText = "Overwrite existing data if asked (with confirmation).")]
    public bool Overwrite { get; set; } = false;

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
        var result = file.Add(ForceRecheck);

        if (result == FileProperties.None) {
            logger.Info($"Successfully added {file}");
            return;
        }

        if (!Overwrite) {
            throw new KifaExecutionException(
                $"Conflict with recorded file info! Please check: {result}");
        }

        var info = file.CalculateInfo(FileProperties.AllVerifiable);
        Console.WriteLine($"{info}\nConfirm overwriting with new data?");
        Console.ReadLine();
        FileInformation.Client.Update(info);
        file.Register(true);
        logger.Info($"Successfully updated data and added {file}.");
    }
}
