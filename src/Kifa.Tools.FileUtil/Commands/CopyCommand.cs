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

[Verb("cp", HelpText = "Copy FILE1 to FILE2. The files will be linked.")]
class CopyCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Min = 2, MetaName = "FILES", MetaValue = "STRING", Required = true,
        HelpText =
            "Files to copy, the last one is the new link name or folder (ending with a slash.")]
    public IEnumerable<string> Files { get; set; }

    public IEnumerable<string> Targets => Files.SkipLast(1);

    public string LinkName => Files.Last();

    [Option('i', "id", HelpText = "Treat all file names as id. And only file ids are linked")]
    public bool ById { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        if (ById) {
            return LinkFile(Targets.First().TrimEnd('/'), LinkName.TrimEnd('/'));
        }

        LinkLocalFile(new KifaFile(Targets.First()), new KifaFile(LinkName));
        return 0;
    }

    static void LinkLocalFile(KifaFile file1, KifaFile file2) {
        file1.Add();
        LinkFile(file1.Id, file2.Id);
        file1.Copy(file2);

        // Skip the full check if the linking is from local file and in the same cell.
        // Caveat: It's only inferred that it used hard linking.
        file2.Register(file1.IsCompatible(file2) && file2.IsLocal);
        file2.Add();
    }

    static int LinkFile(string target, string linkName) {
        if (!target.StartsWith('/') || !linkName.StartsWith('/')) {
            Logger.Error("You should use absolute file path for the two arguments.");
            return 1;
        }

        var files = FileInformation.Client.ListFolder(target, true);
        if (files.Count == 0) {
            if (FileInformation.Client.Get(target)?.Exists != true) {
                Logger.Fatal($"Target {target} not found.");
                return 1;
            }

            if (FileInformation.Client.Get(linkName)?.Exists == true) {
                Logger.Fatal($"Link name {linkName} already exists.");
                return 1;
            }

            Logger.LogResult(FileInformation.Client.Link(target, linkName),
                $"linking {linkName} => {target}!");
        } else {
            foreach (var file in files) {
                var linkFile = linkName + file[target.Length..];
                Console.WriteLine($"{linkFile} => {file}");
            }

            if (!Confirm($"Confirm linking the {files.Count} above?")) {
                Logger.Info("Linking cancelled.");
                return -1;
            }

            foreach (var file in files) {
                var linkFile = linkName + file[target.Length..];
                if (FileInformation.Client.Get(file) == null) {
                    Logger.Warn($"Target {file} not found.");
                    continue;
                }

                if (FileInformation.Client.Get(linkFile) != null) {
                    Logger.Warn($"Link name {linkFile} already exists. Ignored.");
                    continue;
                }

                Logger.LogResult(FileInformation.Client.Link(file, linkFile),
                    $"linking {linkFile} => {file}!");
            }
        }

        return 0;
    }
}
