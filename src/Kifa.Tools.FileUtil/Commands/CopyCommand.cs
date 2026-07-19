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
            "Files to copy, the last one is the destination link name or folder (ending with a slash).")]
    public IEnumerable<string> Files { get; set; }

    public IEnumerable<string> Sources => Files.SkipLast(1);

    public string Destination => Files.Last();

    [Option('i', "id", HelpText = "Treat all file names as id. And only file ids are linked")]
    public bool ById { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        if (ById) {
            return ExecuteById();
        }

        return ExecuteLocal();
    }

    int ExecuteLocal() {
        var destination = new KifaFile(Destination);
        var sourceItems = Sources.Select(s => new KifaFile(s)).ToList();
        var isDestFolder = Destination.EndsWith('/') || sourceItems.Count > 1;

        var localFileCopyPairs = new List<(KifaFile SourceFile, KifaFile DestinationFile)>();

        foreach (var sourceItem in sourceItems) {
            if (!sourceItem.Exists() && sourceItem.List(recursive: true).Any()) {
                var childFiles = sourceItem.List(recursive: true).ToList();
                var sourceFolderId = sourceItem.Id.EndsWith('/') ? sourceItem.Id : sourceItem.Id + "/";
                var baseDestId = isDestFolder
                    ? $"{destination.Host}{destination.Id.TrimEnd('/')}/{sourceItem.Name}"
                    : $"{destination.Host}{destination.Id.TrimEnd('/')}";

                foreach (var childFile in childFiles) {
                    var relativePath = childFile.Id[sourceFolderId.Length..];
                    var targetFile = new KifaFile($"{baseDestId}/{relativePath}");
                    localFileCopyPairs.Add((childFile, targetFile));
                }
            } else {
                var targetFile = isDestFolder
                    ? destination.GetFile(sourceItem.Name)
                    : destination;
                localFileCopyPairs.Add((sourceItem, targetFile));
            }
        }

        var selected = SelectMany(localFileCopyPairs, pair => $"{pair.SourceFile}\n=>\t{pair.DestinationFile}",
            "files to link");

        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("files to link", () => selected);
            return LogSummary();
        }

        foreach (var (sourceFile, destinationFile) in selected.Value) {
            ExecuteItem(sourceFile.ToString(), () => LinkLocalFile(sourceFile, destinationFile));
        }

        return LogSummary();
    }

    int ExecuteById() {
        var destinationPath = Destination.TrimEnd('/');
        var sourceIds = Sources.Select(s => s.TrimEnd('/')).ToList();

        if (sourceIds.Any(s => !s.StartsWith('/')) || !destinationPath.StartsWith('/')) {
            Logger.Error("You should use absolute file path for all arguments.");
            return 1;
        }

        var isDestFolder = Destination.EndsWith('/') || sourceIds.Count > 1;
        var idFileCopyPairs = new List<(string SourceId, string DestinationId)>();

        foreach (var sourceId in sourceIds) {
            var files = FileInformation.Client.ListFolder(sourceId, true);
            if (files.Count > 0) {
                foreach (var childFileId in files) {
                    var relativeDestinationId = isDestFolder
                        ? $"{destinationPath}/{sourceId.Split('/').Last()}{childFileId[sourceId.Length..]}"
                        : destinationPath + childFileId[sourceId.Length..];
                    idFileCopyPairs.Add((childFileId, relativeDestinationId));
                }
            } else {
                var destinationId = isDestFolder
                    ? $"{destinationPath}/{sourceId.Split('/').Last()}"
                    : destinationPath;
                idFileCopyPairs.Add((sourceId, destinationId));
            }
        }

        var selected = SelectMany(idFileCopyPairs, pair => $"{pair.SourceId}\n=>\t{pair.DestinationId}",
            "files to link");

        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("files to link", () => selected);
            return LogSummary();
        }

        foreach (var (sourceId, destinationId) in selected.Value) {
            ExecuteItem(sourceId, () => LinkFileEntry(sourceId, destinationId));
        }

        return LogSummary();
    }

    static KifaActionResult LinkLocalFile(KifaFile sourceFile, KifaFile destinationFile)
        => KifaActionResult.FromAction(() => {
            sourceFile.Add();

            if (destinationFile.Exists()) {
                if (sourceFile.IsLinked(destinationFile)) {
                    var linkResult = FileInformation.Client.Link(sourceFile.Id, destinationFile.Id);
                    if (linkResult.Status != KifaActionStatus.OK) {
                        return linkResult;
                    }

                    destinationFile.Register(true);
                    destinationFile.Add();

                    return KifaActionResult.Success(
                        $"File {destinationFile} is already linked to {sourceFile} on disk.");
                }

                var destinationSha256 = destinationFile.FileInfo?.Sha256 ??
                                        destinationFile.CalculateInfo(FileProperties.Sha256).Sha256;
                var isSameContent = sourceFile.FileInfo?.Sha256 != null &&
                                    destinationSha256 == sourceFile.FileInfo.Sha256;

                if (isSameContent) {
                    var linkResult = FileInformation.Client.Link(sourceFile.Id, destinationFile.Id);
                    if (linkResult.Status != KifaActionStatus.OK) {
                        return linkResult;
                    }

                    destinationFile.Register(true);
                    destinationFile.Add();

                    return KifaActionResult.Warning(
                        $"File {destinationFile} already exists as a different instance on disk (same content). Run 'filex clean' to deduplicate.");
                }

                return KifaActionResult.Error(
                    $"File {destinationFile} already exists as a different instance on disk (different content).");
            }

            var result = FileInformation.Client.Link(sourceFile.Id, destinationFile.Id);
            if (result.Status != KifaActionStatus.OK) {
                return result;
            }

            sourceFile.Copy(destinationFile);

            // Skip the full check if the linking is from local file and in the same cell.
            // Caveat: It's only inferred that it used hard linking.
            destinationFile.Register(true);
            destinationFile.Add();
            return KifaActionResult.Success();
        });

    static KifaActionResult LinkFileEntry(string sourceId, string destinationId) {
        if (destinationId.EndsWith('/')) {
            destinationId = $"{destinationId.TrimEnd('/')}/{sourceId.Split('/').Last()}";
        }

        return FileInformation.Client.Link(sourceId, destinationId);
    }
}
