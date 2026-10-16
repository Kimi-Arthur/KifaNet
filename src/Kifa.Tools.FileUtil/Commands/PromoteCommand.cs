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

[Verb("promote", HelpText = "Promote hidden file(s) (starting with default ignored prefix) to normal file(s).")]
public class PromoteCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to promote.")]
    public IEnumerable<string> FileNames { get; set; } = [];

    [Option('S', "show-size", HelpText = "Show size for each file and total size (can be slow).")]
    public bool ShowSize { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var files = KifaFile.FindExistingFiles(FileNames, shouldIgnoreFiles: false)
            .Where(f => f.IsIgnoredFile)
            .ToList();

        var selected = SelectMany(files,
            file => ShowSize ? $"{file} ({file.Length.ToSizeString()})" : file.ToString(),
            new Func<List<KifaFile>, string>(choices
                => $"files{(ShowSize ? $" ({choices.Sum(c => c.Length).ToSizeString()})" : "")} to promote to normal files"));

        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("files to promote", () => selected);
            return LogSummary();
        }

        foreach (var file in selected.Value.Checked()) {
            ExecuteItem(file.ToString(), () => PromoteFile(file));
            file.Dispose();
        }

        return LogSummary();
    }

    public static KifaActionResult PromoteFile(KifaFile file) {
        if (!file.IsIgnoredFile) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"{file} is not an ignored file."
            };
        }

        if (!file.Exists()) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Source file {file} doesn't exist."
            };
        }

        var targetFile = file.GetPromotedFile();
        if (targetFile.Exists()) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Target file {targetFile} already exists."
            };
        }

        var isRegistered = file.Registered;
        file.Move(targetFile);

        if (isRegistered) {
            if (file.Id != targetFile.Id) {
                FileInformation.Client.Link(file.Id, targetFile.Id);
            }

            targetFile.Register(true);
            file.Unregister();
            FileInformation.Client.Delete(file.Id);
        }

        return new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message = $"Successfully promoted {file} to {targetFile}."
        };
    }
}
