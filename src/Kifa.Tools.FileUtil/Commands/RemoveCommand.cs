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

[Verb("rm",
    HelpText =
        "Remove the FILE. Can be either logic path like: /Software/... or real path like: local:desk/Software....")]
class RemoveCommand : KifaFileCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to remove.")]
    public IEnumerable<string> FileNames {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    [Option('i', "id", HelpText = "Delete file by id instead of just instances.")]
    public virtual bool ById { get; set; } = false;

    [Option('l', "link", HelpText = "Remove link only.")]
    public bool RemoveLinkOnly { get; set; }

    [Option('a', "all-references", HelpText = "Remove all references.")]
    public bool RemoveAllReferences { get; set; }

    [Option('f', "force",
        HelpText =
            "Force remove files even if no other instances exist. Only use when a file is actually removed.")]
    public bool Force { get; set; }

    [Option('S', "show-size", HelpText = "Show size for each file and total size (can be slow).")]
    public bool ShowSize { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        FileNames = FileNames.ToList();
        var removalText = RemoveLinkOnly ? "" : " and remove them from file system";

        if (RemoveAllReferences && !ById) {
            Logger.Fatal("-a can only be used with -i");
            return 1;
        }

        if (ById) {
            var fileInfos = new List<FileInformation>();
            foreach (var fileName in FileNames) {
                if (!fileName.StartsWith('/')) {
                    fileInfos.Clear();
                    break;
                }

                fileInfos.AddRange(FileInformation.Client.List(folder: fileName,
                    options: new KifaDataOptions {
                        Fields = ["Id", "Metadata", "Size"]
                    }).Values);
            }

            // We support relative paths or FileInformation ids.
            if (fileInfos.Count == 0) {
                var foundFiles = KifaFile.FindAllFiles(FileNames);
                if (foundFiles.Count == 0) {
                    Logger.Fatal("No files found!");
                    return 1;
                }

                fileInfos.AddRange(foundFiles.Select(file => file.FileInfo.Checked()));
            }

            if (RemoveAllReferences) {
                fileInfos = fileInfos.SelectMany(f => f.GetAllLinks()).Distinct().Select(f
                    => new FileInformation {
                        Id = f
                    }).ToList();
            }

            var selected = SelectMany(fileInfos,
                file => ShowSize && file.Size != null
                    ? $"{file.Id} ({file.Size.ToSizeString()})"
                    : file.Id,
                new Func<List<FileInformation>, string>(choices
                    => $"file entries{(ShowSize ? $" ({choices.Sum(c => c.Size ?? 0).ToSizeString()})" : "")} to remove along all relevant instances"));

            if (selected.Status != KifaActionStatus.OK) {
                ExecuteItem("file entries to remove", () => selected);
                return LogSummary();
            }

            if (Force && !Confirm(
                    "Since --force is specified, files of the only version will automatically be removed!\nIt will truly remove files from everywhere!!! Do you want to continue?")) {
                Logger.Warn("Action canceled.");
                return 2;
            }

            selected.Value.ForEach(f => ExecuteItem(f.Id.Checked(),
                () => KifaFile.RemoveLogical(f.Id, RemoveLinkOnly, Force)));
            return LogSummary();
        }

        var localFiles = KifaFile.FindExistingFiles(FileNames);

        if (localFiles.Count > 0) {
            var selected = SelectMany(localFiles,
                file => ShowSize ? $"{file} ({file.Length.ToSizeString()})" : file.ToString(),
                new Func<List<KifaFile>, string>(choices
                    => $"local files{(ShowSize ? $" ({choices.Sum(c => c.Length).ToSizeString()})" : "")} to delete{removalText}"));

            if (selected.Status == KifaActionStatus.OK) {
                selected.Value.ForEach(f => ExecuteItem(f.ToString(),
                    () => f.RemoveInstance(RemoveLinkOnly)));
            } else {
                ExecuteItem("local files to delete", () => selected);
            }
        }

        var phantomFiles = KifaFile.FindPhantomFiles(FileNames);
        if (phantomFiles.Count > 0) {
            foreach (var file in phantomFiles) {
                Console.WriteLine(file);
            }

            if (!Confirm(
                    $"Confirm deleting the {phantomFiles.Count} phantom files above{removalText}?")) {
                Logger.Info("Action canceled.");
                return 2;
            }

            phantomFiles.ForEach(f => ExecuteItem(f.ToString(),
                () => f.RemoveInstance(RemoveLinkOnly)));
        }

        return LogSummary();
    }
}
