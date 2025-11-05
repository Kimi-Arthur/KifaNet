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

    KifaActionResult<List<string>> LinkFile(KifaFile file) {
        var video = BilibiliVideo.Parse(file.Id);
        if (video.video == null) {
            return new KifaActionResult<List<string>> {
                Status = KifaActionStatus.Error,
                Message = "Video info not found."
            };
        }

        var canonicalNames = video.video.GetCanonicalNames(video.pid, video.quality, video.codec);
        var linkedFiles = new List<string>();
        foreach (var canonicalName in canonicalNames) {
            var canonicalFile =
                GetCanonicalFile(CurrentFolder.Host, $"{canonicalName}.{file.Extension}");
            if (canonicalFile.Equals(file)) {
                Logger.Info($"Skipped {canonicalFile} as it's the source file.");
                linkedFiles.Add($"{canonicalFile} is source file.");
                continue;
            }

            if (canonicalFile.Exists() && canonicalFile.Length == file.Length) {
                Logger.Info(
                    $"Source file is not canonical file, but canonical file {canonicalFile} exists too. Skipped.");
                linkedFiles.Add($"{canonicalFile} exists.");
                continue;
            }

            file.Copy(canonicalFile);
            Logger.Info(
                $"Source file is not canonical file. Linked to canonical file {canonicalFile}.");
            linkedFiles.Add($"Copied to {canonicalFile}");
        }

        return linkedFiles;
    }
}
