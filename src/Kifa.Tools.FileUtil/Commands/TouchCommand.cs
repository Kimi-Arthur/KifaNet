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

[Verb("touch", HelpText = "Touch file.")]
class TouchCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, MetaName = "File URL")]
    public string FileUri { get; set; }

    [Option('S', "show-size", HelpText = "Show size for each file and total size (can be slow).")]
    public bool ShowSize { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var target = new KifaFile(FileUri);

        var files = FileInformation.Client.ListFolder(target.Id, true);
        if (files.Count > 0) {
            var selected = SelectMany(files.Select(f => new KifaFile(target.Host + f)).ToList(),
                f => ShowSize && f.FileInfo?.Size != null
                    ? $"{f} ({f.FileInfo.Size.ToSizeString()})"
                    : f.ToString(),
                new Func<List<KifaFile>, string>(choices
                    => $"files{(ShowSize ? $" ({choices.Sum(c => c.FileInfo?.Size ?? 0).ToSizeString()})" : "")} to touch"));
            if (selected.Status == KifaActionStatus.OK) {
                foreach (var file in selected.Value) {
                    ExecuteItem(file.ToString(), () => TouchFile(file));
                }
            } else {
                ExecuteItem("files to touch", () => selected);
            }
        } else {
            ExecuteItem(target.ToString(), () => TouchFile(target));
        }

        return LogSummary();
    }

    static KifaActionResult TouchFile(KifaFile target) {
        if (target.Exists()) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = $"{target} already exists!"
            };
        }

        target.Touch();

        if (target.Exists()) {
            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"{target} is successfully touched!"
            };
        }

        return new KifaActionResult {
            Status = KifaActionStatus.Error,
            Message = $"{target} doesn't exist unexpectedly!"
        };
    }
}
