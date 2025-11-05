using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("link", HelpText = "Link bilibili files to canonical places.")]
public class LinkCommand : BiliCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target files to link.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute(KifaTask? task = null) {
        var selectedFiles = SelectMany(KifaFile.FindExistingFiles(FileNames, pattern: "*.mp4"));
        if (selectedFiles.Count == 0) {
            Logger.Warn("No files found or selected to link.");
            return 1;
        }

        foreach (var file in selectedFiles) {
            ExecuteItem(file.ToString(), () => LinkFile(file));
        }

        return LogSummary();
    }

    static KifaActionResult LinkFile(KifaFile file) {
        var video = BilibiliVideo.Parse(file.Id);
        if (video.video == null) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Video info not found for {file.Id}."
            };
        }

        var canonicalNames = video.video.GetCanonicalNames(video.pid, video.quality, video.codec);
        var results = new KifaBatchActionResult();
        foreach (var canonicalName in canonicalNames) {
            var canonicalFile =
                GetCanonicalFile(CurrentFolder.Host, $"{canonicalName}.{file.Extension}");
            if (canonicalFile.Equals(file)) {
                Logger.Info($"Skipped {canonicalFile} as it's the source file.");
                results.Add(canonicalFile.ToString(), new KifaActionResult {
                    Status = KifaActionStatus.Skipped,
                    Message = "Is same file as source."
                });
                continue;
            }

            if (canonicalFile.Exists()) {
                if (canonicalFile.IsSameLocalFile(file)) {
                    results.Add(canonicalFile.ToString(), new KifaActionResult {
                        Status = KifaActionStatus.Skipped,
                        Message = "Is already linked to the source."
                    });
                    continue;
                }

                results.Add(canonicalFile.ToString(), new KifaActionResult {
                    Status = KifaActionStatus.Error,
                    Message = "Cannot link as the target exists and is not the same file."
                });
                continue;
            }

            results.Add(canonicalFile.ToString(),
                KifaActionResult.FromAction(() => file.Copy(canonicalFile), "Linked to source"));
        }

        return results;
    }
}
