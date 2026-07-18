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
            var target = Targets.First().TrimEnd('/');
            var linkName = LinkName.TrimEnd('/');

            if (!target.StartsWith('/') || !linkName.StartsWith('/')) {
                Logger.Error("You should use absolute file path for the two arguments.");
                return 1;
            }

            var files = FileInformation.Client.ListFolder(target, true);
            if (files.Count > 0) {
                return ExecuteByIdFolder(target, linkName, files);
            }

            return ExecuteByIdFile(target, LinkName);
        }

        var source = new KifaFile(Targets.First());
        var destination = new KifaFile(LinkName);

        if (!source.Exists() && source.List(recursive: true).Any()) {
            return ExecuteLocalFolder(source, destination);
        }

        return ExecuteLocalFile(source, destination);
    }

    int ExecuteLocalFolder(KifaFile source, KifaFile destination) {
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

        return LogSummary();
    }

    int ExecuteLocalFile(KifaFile source, KifaFile destination) {
        var targetFile = destination.Id.EndsWith('/') || LinkName.EndsWith('/')
            ? destination.GetFile(source.Name)
            : destination;

        var pair = (Source: source, Destination: targetFile);
        var selected = SelectMany(new List<(KifaFile Source, KifaFile Destination)> { pair },
            p => $"{p.Source}\n=>\t{p.Destination}", "files to link");

        if (selected.Count == 0) {
            Logger.Warn("No files selected to link.");
            return 0;
        }

        foreach (var (file, dest) in selected) {
            ExecuteItem(file.ToString(), () => LinkLocalFile(file, dest));
        }

        return LogSummary();
    }

    int ExecuteByIdFolder(string target, string linkName, List<string> files) {
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

    int ExecuteByIdFile(string target, string linkName) {
        var targetLink = linkName.EndsWith('/')
            ? $"{linkName.TrimEnd('/')}/{target.Split('/').Last()}"
            : linkName;

        var link = (File: target, Link: targetLink);
        var selected = SelectMany(new List<(string File, string Link)> { link },
            l => $"{l.File}\n=>\t{l.Link}", "files to link");

        if (selected.Count == 0) {
            Logger.Warn("No files selected to link.");
            return 0;
        }

        foreach (var (file, linkPath) in selected) {
            ExecuteItem(file, () => LinkFileEntry(file, linkPath));
        }

        return LogSummary();
    }

    static KifaActionResult LinkLocalFile(KifaFile file1, KifaFile file2)
        => KifaActionResult.FromAction(() => {
            if (file2.Id.EndsWith('/')) {
                file2 = file2.GetFile(file1.Name);
            }

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

    static KifaActionResult LinkFileEntry(string file, string link) {
        if (link.EndsWith('/')) {
            link = $"{link.TrimEnd('/')}/{file.Split('/').Last()}";
        }

        return FileInformation.Client.Link(file, link);
    }
}
