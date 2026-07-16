using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("normalize", HelpText = "Rename the file with proper normalization.")]
class NormalizeCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to normalize.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('S', "show-size", HelpText = "Show size for each file and total size (can be slow).")]
    public bool ShowSize { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var files = KifaFile.FindExistingFiles(FileNames);
        var selected = SelectMany(files, file => ShowSize ? $"{file} ({file.FileInfo?.Size.ToSizeString()})" : file.ToString(),
            new Func<List<KifaFile>, string>(choices
                => $"files{(ShowSize ? $" ({choices.Sum(c => c.FileInfo?.Size ?? 0).ToSizeString()})" : "")} to normalize their paths (FormC + extension to lower)"));

        foreach (var file in selected) {
            ExecuteItem(file.ToString(), () => NormalizeFile(file));
            file.Dispose();
        }

        return LogSummary();
    }

    KifaActionResult NormalizeFile(KifaFile file) {
        // This action is very local. Just stick to the local way of doing things.
        var path = file.GetLocalPath();
        var segments = path.Split(".");
        if (path.IsNormalized(NormalizationForm.FormC) && segments[^1].ToLower() == segments[^1]) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = $"{file} is already normalized."
            };
        }

        segments[^1] = segments[^1].ToLower();

        var newPath = string.Join(".", segments.Select(s => s.Normalize(NormalizationForm.FormC)));
        file.Move(new KifaFile(newPath));
        return new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message = $"Successfully normalized {path} to {newPath}."
        };
    }
}
