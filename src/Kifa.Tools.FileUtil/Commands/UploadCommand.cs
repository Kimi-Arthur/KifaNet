using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("upload", HelpText = "Upload file to a cloud location.")]
class UploadCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static List<string> DefaultTargets { get; set; }

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('d', "delete-source",
        HelpText = "Remove source if upload is successful. Won't remove valid cloud version.")]
    public bool DeleteSource { get; set; } = false;

    [Option('q', "quick", HelpText = "Finish quickly by not verifying validity of destination.")]
    public bool QuickMode { get; set; } = false;

    [Option('t', "targets",
        HelpText =
            "Targets to upload to, in the format of 'google.v1', 'swiss.v2' or combined 'google.v1,swiss.v2' etc.")]
    public string Targets { get; set; } = "";

    [Option('c', "use-cache", HelpText = "Use cache to help upload.")]
    public bool UseCache { get; set; } = false;

    [Option('l', "download-local", HelpText = "Download the file to local.")]
    public bool DownloadLocal { get; set; } = false;

    [Option('s', "skip-uploaded", HelpText = "Skip potentially uploaded files.")]
    public bool SkipPotentiallyUploadFiles { get; set; } = false;

    [Option('a', "include-all", HelpText = "Include all files already registered.")]
    public bool IncludeAll { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var targetsFromFlag = Targets.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();

        var targets = (targetsFromFlag.Count == 0 ? DefaultTargets : targetsFromFlag)
            .Select(CloudTarget.Parse).ToList();

        var files = KifaFile.FindExistingFiles(FileNames);
        if (IncludeAll) {
            files.AddRange(KifaFile.FindPotentialFiles(FileNames, ignoreFiles: false)
                .Where(f => f.Exists()));
        }

        var verifyText = QuickMode ? " without verification" : "";
        var downloadText = DownloadLocal ? " and download to local" : "";
        var removalText = DeleteSource ? " and remove source afterwards" : "";

        // TODO: somehow make this type auto inferred.
        var selected = SelectMany(files, file => $"{file} ({file.Length.ToSizeString()})",
            new Func<List<KifaFile>, string>(choices
                => $"files ({choices.Sum(c => c.Length).ToSizeString()}) to {string.Join(", ", targets)}{verifyText}{downloadText}{removalText}"));

        if (selected.Count == 0) {
            Logger.Warn("No files found or selected.");
            return 0;
        }

        foreach (var file in selected) {
            ExecuteItem(file.ToString(),
                () => new KifaFile(file.ToString()).Upload(targets, DeleteSource, UseCache,
                    DownloadLocal, QuickMode, true));
        }

        var pendingFiles = PopPendingResults().Select(item => item.item);
        if (SkipPotentiallyUploadFiles) {
            foreach (var file in pendingFiles) {
                ExecuteItem(file, () => new KifaActionResult {
                    Status = KifaActionStatus.OK,
                    Message = "File skipped as it's uploaded, though not verified."
                });
            }
        } else {
            // TODO: batch get FileInformation.
            foreach (var file in pendingFiles) {
                ExecuteItem(file,
                    () => new KifaFile(file).Upload(targets, DeleteSource, UseCache, DownloadLocal,
                        QuickMode));
            }
        }

        // TODO: Need to print recheck command for QuickMode.
        return LogSummary();
    }
}
