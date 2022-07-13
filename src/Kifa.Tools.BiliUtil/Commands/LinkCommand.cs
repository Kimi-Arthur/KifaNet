using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("link", HelpText = "Link bilibili files to proper places.")]
public class LinkCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target files to link.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute() {
        var (_, foundFiles) = KifaFile.FindExistingFiles(FileNames, pattern: "*.mp4");
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
                Logger.Info($"\t{file} => {result.Response}");
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

    KifaActionResult<KifaFile> LinkFile(KifaFile file) {
        var video = Helper.GetVideo(file.Id);
        var canonicalName = video.video.GetCanonicalName(video.pid, video.quality);
        if (file.Equals(CurrentFolder.GetFile(canonicalName))) {
            Logger.Debug("Source file is canonical file. Link to desired name.");
            // Skip for now.
            return file;
        } else {
            var target = CurrentFolder.GetFile($"{canonicalName}.{file.Extension}");
            if (target.Exists() && target.Length() == file.Length()) {
                Logger.Debug($"Source file is not canonical file. And canonical file {target} exists too. Skipped.");
                return target;
            }

            file.Copy(target);
            Logger.Debug($"Source file is not canonical file. Linked to canonical file {target}.");
            return target;
        }
    }
}
