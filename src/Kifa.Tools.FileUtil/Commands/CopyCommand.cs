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

    void LinkLocalFile(KifaFile file1, KifaFile file2) {
        file1.Add();
        LinkFile(file1.Id, file2.Id);
        file1.Copy(file2);

        // Skip the full check if the linking is from local file and in the same cell.
        // Caveat: It's only inferred that it used hard linking.
        file2.Register(file1.IsCompatible(file2) && file2.IsLocal);
        file2.Add();
    }

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
        var selected = SelectMany(links, link => $"\t{link.File}\n->\t{link.Link}",
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
