using System;
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

    public override int Execute(KifaTask? task = null) {
        var target = new KifaFile(FileUri);

        var files = FileInformation.Client.ListFolder(target.Id, true);
        if (files.Count > 0) {
            var selected = SelectMany(files.Select(f => new KifaFile(target.Host + f)).ToList(),
                f => f.ToString(), "files to touch");
            foreach (var file in selected) {
                ExecuteItem(file.ToString(), () => TouchFile(file));
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
