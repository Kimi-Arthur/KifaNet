using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
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

    public override int Execute() {
        var targetsFromFlag = Targets.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();

        var targets = (targetsFromFlag.Count == 0 ? DefaultTargets : targetsFromFlag)
            .Select(CloudTarget.Parse).ToList();

        var files = KifaFile.FindExistingFiles(FileNames);
        if (IncludeAll) {
            files.AddRange(KifaFile.FindPotentialFiles(FileNames, ignoreFiles: false)
                .Where(f => f.Exists()));
        }

        foreach (var file in files) {
            Console.WriteLine(file);
        }

        var verifyText = QuickMode ? " without verification" : "";
        var downloadText = DownloadLocal ? " and download to local" : "";
        var removalText = DeleteSource ? " and remove source afterwards" : "";
        Console.Write(
            $"Confirm uploading the {files.Count} files above to {string.Join(", ", targets)}{verifyText}{downloadText}{removalText}?");
        Console.ReadLine();

        foreach (var file in files) {
            ExecuteItem(file.ToString(),
                () => new KifaFile(file.ToString()).Upload(targets, DeleteSource, UseCache,
                    DownloadLocal, QuickMode, true));
        }

        var pendingFiles = PopPendingResults().Select(item => new KifaFile(item.item));
        if (SkipPotentiallyUploadFiles) {
            foreach (var file in pendingFiles) {
                ExecuteItem(file.ToString(), () => new KifaActionResult {
                    Status = KifaActionStatus.OK,
                    Message = "File skipped as it's potentially uploaded."
                });
            }
        } else {
            foreach (var file in pendingFiles) {
                ExecuteItem(file.ToString(),
                    () => file.Upload(targets, DeleteSource, UseCache, DownloadLocal, QuickMode));
            }
        }

        // TODO: Quick mode hint text is not printed. Maybe a better approach later.
        return LogSummary();
    }
}
