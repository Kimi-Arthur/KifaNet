using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("link", HelpText = "Link bilibili files to proper places.")]
public class LinkCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target files to link.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute() {
        var foundFiles = KifaFile.FindExistingFiles(FileNames, pattern: "*.mp4");
        foreach (var file in foundFiles) {
            Console.WriteLine(file);
        }

        if (!Confirm($"Confirm linking files for the {foundFiles.Count} files above")) {
            return 1;
        }

        var filesByResult = foundFiles.Select(file => (file, result: LinkFile(file)))
            .GroupBy(item => item.result.Status == KifaActionStatus.OK)
            .ToDictionary(item => item.Key, item => item.ToList());

        if (filesByResult.ContainsKey(true)) {
            var files = filesByResult[true];
            Logger.Info($"Successfully linked files for the following {files.Count} files:");
            foreach (var (file, result) in files) {
                Logger.Info($"\t{file} =>");
                foreach (var message in result.Response) {
                    Logger.Info($"\t\t{message}");
                }
            }
        }

        if (filesByResult.ContainsKey(false)) {
            var files = filesByResult[false];
            Logger.Info($"Failed to link files for the following {files.Count} files:");
            foreach (var (file, result) in files) {
                Logger.Info($"\t{file}: {result.Message}");
            }

            return 1;
        }

        return 0;
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
            var canonicalFile = CurrentFolder.GetFile($"{canonicalName}.{file.Extension}");
            if (canonicalFile.Equals(file)) {
                Logger.Info($"Skipped {canonicalFile} as it's the source file.");
                linkedFiles.Add($"{canonicalFile} is source file.");
                continue;
            }

            if (canonicalFile.Exists() && canonicalFile.Length() == file.Length()) {
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
