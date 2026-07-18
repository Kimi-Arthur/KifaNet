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

        var source = new KifaFile(Targets.First());
        var destination = new KifaFile(LinkName);

        if (!source.Exists() && source.List(recursive: true).Any()) {
            var files = source.List(recursive: true).ToList();
            var sourceFolderId = source.Id.EndsWith('/') ? source.Id : source.Id + "/";
            var pairs = files.Select(file => {
                var relativePath = file.Id[sourceFolderId.Length..];
                var targetFile = new KifaFile($"{destination.Host}{destination.Id.TrimEnd('/')}/{relativePath}");
                return (Source: file, Destination: targetFile);
            }).ToList();

            var selected = SelectMany(pairs, pair => $"{pair.Source}\n=>\t{pair.Destination}",
                "files to link");

            if (selected.Count == 0) {
                Logger.Warn("No files selected to link.");
                return 0;
            }

            foreach (var (file, targetFile) in selected) {
                ExecuteItem(file.ToString(), () => LinkLocalFile(file, targetFile));
            }
        } else {
            ExecuteItem(source.ToString(), () => LinkLocalFile(source, destination));
        }

        return LogSummary();
    }

    KifaActionResult LinkLocalFile(KifaFile file1, KifaFile file2)
        => KifaActionResult.FromAction(() => {
            file1.Add();
            var result = FileInformation.Client.Link(file1.Id, file2.Id);
            if (result.Status != KifaActionStatus.OK) {
                return result;
            }

            file1.Copy(file2);

            // Skip the full check if the linking is from local file and in the same cell.
            // Caveat: It's only inferred that it used hard linking.
            file2.Register(file1.IsCompatible(file2) && file2.IsLocal);
            file2.Add();
            return KifaActionResult.Success();
        });

    int LinkFile(string target, string linkName) {
        if (!target.StartsWith('/') || !linkName.StartsWith('/')) {
            Logger.Error("You should use absolute file path for the two arguments.");
            return 1;
        }

        var files = FileInformation.Client.ListFolder(target, true);
        if (files.Count == 0) {
            Logger.Fatal($"Target {target} not found.");
            return 1;
        }

        var links = files.Select(file => (File: file, Link: linkName + file[target.Length..]))
            .ToList();
        var selected = SelectMany(links, link => $"{link.File}\n=>\t{link.Link}",
            "files to link");

        if (selected.Count == 0) {
            Logger.Warn("No files selected to link.");
            return 0;
        }

        foreach (var (file, link) in selected) {
            ExecuteItem(file, () => LinkFileEntry(file, link));
        }

        return LogSummary();
    }

    static KifaActionResult LinkFileEntry(string file, string link)
        => FileInformation.Client.Link(file, link);
}
