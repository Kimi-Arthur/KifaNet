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
public class UploadCommand : KifaCommand {
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
            "Targets to upload to, in the format of 'google.v1', 'tele.v2' or combined 'google.v1,tele.v2' etc.")]
    public string Targets { get; set; } = "";

    [Option('c', "use-cache", HelpText = "Use cache to help upload.")]
    public bool UseCache { get; set; } = false;

    [Option('l', "download-local", HelpText = "Download the file to local.")]
    public bool DownloadLocal { get; set; } = false;

    [Option('s', "skip-uploaded", HelpText = "Skip potentially uploaded files.")]
    public bool SkipPotentiallyUploadFiles { get; set; } = false;

    [Option('a', "include-all", HelpText = "Include all files already registered.")]
    public bool IncludeAll { get; set; } = false;

    [Option('S', "show-size", HelpText = "Show size for each file and total size (can be slow).")]
    public bool ShowSize { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var targetsFromFlag = Targets.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();

        var targets = (targetsFromFlag.Count == 0 ? DefaultTargets : targetsFromFlag)
            .Select(CloudTarget.Parse).ToList();

        var files = KifaFile.FindExistingFiles(FileNames, shouldIgnoreFiles: !IncludeAll);

        var verifyText = QuickMode ? " without verification" : "";
        var downloadText = DownloadLocal ? " and download to local" : "";
        var removalText = DeleteSource ? " and remove source afterwards" : "";

        var selected = SelectMany(files, file => ShowSize ? $"{file} ({file.Length.ToSizeString()})" : file.ToString(),
            new Func<List<KifaFile>, string>(choices
                => $"files{(ShowSize ? $" ({choices.Sum(c => c.Length).ToSizeString()})" : "")} to {string.Join(", ", targets)}{verifyText}{downloadText}{removalText}"));

        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("files to upload", () => selected);
            return LogSummary();
        }

        foreach (var file in selected.Value.Checked()) {
            // Re-instantiate KifaFile so the latest FileInfo is fetched from server, avoiding
            // duplicate uploads if earlier items in the batch updated or linked file information.
            ExecuteItem(file.ToString(),
                () => new KifaFile(file.ToString()).Upload(targets, DeleteSource, UseCache,
                    DownloadLocal, QuickMode, true, (prompt, suggested) => Confirm(prompt, suggested)));
        }

        var pendingResults = PopPendingResults();
        if (SkipPotentiallyUploadFiles) {
            foreach (var (file, result) in pendingResults) {
                ExecuteItem(file, () => {
                    if (result is KifaBatchActionResult batchResult) {
                        foreach (var item in batchResult.Results) {
                            if (item.Result.Status.HasFlag(KifaActionStatus.Pending)) {
                                item.Result = new KifaActionResult {
                                    Status = KifaActionStatus.Skipped,
                                    Message = "File skipped as it's uploaded, though not verified."
                                };
                            }
                        }

                        if (batchResult.IsAcceptable && DownloadLocal) {
                            batchResult.Add("local", new KifaFile(file).LocalMirrorFile.GetFile());
                        }

                        return batchResult;
                    }

                    return new KifaActionResult {
                        Status = KifaActionStatus.Skipped,
                        Message = "File skipped as it's uploaded, though not verified."
                    };
                });
            }
        } else {
            // TODO: batch get FileInformation.
            foreach (var (file, result) in pendingResults) {
                ExecuteItem(file, () => {
                    if (result is KifaBatchActionResult batchResult) {
                        var pendingTargetNames = batchResult.Results
                            .Where(r => r.Result.Status.HasFlag(KifaActionStatus.Pending))
                            .Select(r => r.Item).ToHashSet();
                        var pendingTargets = targets
                            .Where(t => pendingTargetNames.Contains(t.ToString())).ToList();

                        var pass2Result = new KifaFile(file).Upload(
                            pendingTargets.Count > 0 ? pendingTargets : targets, DeleteSource,
                            UseCache, DownloadLocal, QuickMode, false,
                            (prompt, suggested) => Confirm(prompt, suggested));

                        if (pass2Result is KifaBatchActionResult pass2BatchResult) {
                            foreach (var (item, itemResult) in pass2BatchResult.Results) {
                                var existing = batchResult.Results.FirstOrDefault(r => r.Item == item);
                                if (existing != null) {
                                    existing.Result = itemResult;
                                } else {
                                    batchResult.Add(item, itemResult);
                                }
                            }

                            return batchResult;
                        }

                        return pass2Result;
                    }

                    return new KifaFile(file).Upload(targets, DeleteSource, UseCache,
                        DownloadLocal, QuickMode, false,
                        (prompt, suggested) => Confirm(prompt, suggested));
                });
            }
        }

        // TODO: Need to print recheck command for QuickMode.
        return LogSummary();
    }
}
