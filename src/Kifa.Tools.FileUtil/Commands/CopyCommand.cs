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
            return ExecuteById();
        }

        return ExecuteLocal();
    }

    int ExecuteLocal() {
        var destination = new KifaFile(LinkName);
        var targetList = Targets.Select(t => new KifaFile(t)).ToList();
        var isDestFolder = LinkName.EndsWith('/') || targetList.Count > 1;

        var allPairs = new List<(KifaFile Source, KifaFile Destination)>();

        foreach (var source in targetList) {
            if (!source.Exists() && source.List(recursive: true).Any()) {
                var files = source.List(recursive: true).ToList();
                var sourceFolderId = source.Id.EndsWith('/') ? source.Id : source.Id + "/";
                var baseDestId = isDestFolder
                    ? $"{destination.Host}{destination.Id.TrimEnd('/')}/{source.Name}"
                    : $"{destination.Host}{destination.Id.TrimEnd('/')}";

                foreach (var file in files) {
                    var relativePath = file.Id[sourceFolderId.Length..];
                    var targetFile = new KifaFile($"{baseDestId}/{relativePath}");
                    allPairs.Add((file, targetFile));
                }
            } else {
                var targetFile = isDestFolder
                    ? destination.GetFile(source.Name)
                    : destination;
                allPairs.Add((source, targetFile));
            }
        }

        var selected = SelectMany(allPairs, pair => $"{pair.Source}\n=>\t{pair.Destination}",
            "files to link");

        if (selected.Count == 0) {
            Logger.Warn("No files selected to link.");
            return 0;
        }

        foreach (var (file, dest) in selected) {
            ExecuteItem(file.ToString(), () => LinkLocalFile(file, dest));
        }

        return LogSummary();
    }

    int ExecuteById() {
        var linkName = LinkName.TrimEnd('/');
        var targetList = Targets.Select(t => t.TrimEnd('/')).ToList();

        if (targetList.Any(t => !t.StartsWith('/')) || !linkName.StartsWith('/')) {
            Logger.Error("You should use absolute file path for all arguments.");
            return 1;
        }

        var isLinkNameFolder = LinkName.EndsWith('/') || targetList.Count > 1;
        var allLinks = new List<(string File, string Link)>();

        foreach (var target in targetList) {
            var files = FileInformation.Client.ListFolder(target, true);
            if (files.Count > 0) {
                foreach (var file in files) {
                    var relativeLink = isLinkNameFolder
                        ? $"{linkName}/{target.Split('/').Last()}{file[target.Length..]}"
                        : linkName + file[target.Length..];
                    allLinks.Add((file, relativeLink));
                }
            } else {
                var targetLink = isLinkNameFolder
                    ? $"{linkName}/{target.Split('/').Last()}"
                    : linkName;
                allLinks.Add((target, targetLink));
            }
        }

        var selected = SelectMany(allLinks, link => $"{link.File}\n=>\t{link.Link}",
            "files to link");

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
