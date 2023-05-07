using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("cover", HelpText = "Get Bilibili cover along side existing video files.")]
public class GetCoverCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target files to download cover images for.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute() {
        var foundFiles = KifaFile.FindExistingFiles(FileNames, pattern: "*.mp4");
        foreach (var file in foundFiles) {
            Console.WriteLine(file);
        }

        if (!Confirm($"Confirm getting cover files for the {foundFiles.Count} files above")) {
            return 1;
        }

        var filesByResult = foundFiles.Select(file => (file, result: GetCover(file)))
            .GroupBy(item => item.result.Status == KifaActionStatus.OK)
            .ToDictionary(item => item.Key, item => item.ToList());

        if (filesByResult.ContainsKey(true)) {
            var files = filesByResult[true];
            Logger.Info($"Successfully got cover files for the following {files.Count} files:");
            foreach (var (file, result) in files) {
                Logger.Info($"\t{file} => {result.Response}");
            }
        }

        if (filesByResult.ContainsKey(false)) {
            var files = filesByResult[false];
            Logger.Info($"Failed to get cover files for the following {files.Count} files:");
            foreach (var (file, result) in files) {
                Logger.Info($"\t{file}: {result.Message}");
            }

            return 1;
        }

        return 0;
    }

    static readonly List<string> ExpectedCoverExtensions = new() {
        "jpg",
        "png"
    };

    static KifaActionResult<KifaFile> GetCover(KifaFile file)
        => KifaActionResult<KifaFile>.FromAction(() => {
            var foundCoverExtension = ExpectedCoverExtensions.FirstOrDefault(ext
                => file.Parent.GetFile($"{file.BaseName}.{ext}").Exists());
            if (foundCoverExtension != null) {
                var foundCoverFile = file.Parent.GetFile($"{file.BaseName}.{foundCoverExtension}");
                Logger.Info($"Found cover file {foundCoverFile} for {file}. Skipped.");
                return file.Parent.GetFile($"{file.BaseName}.{foundCoverExtension}");
            }

            var video = BilibiliVideo.Parse(file.Path);
            if (video.video == null) {
                throw new Exception($"Video not found for {file.Path}.");
            }

            var coverLink = video.video.Cover.ToString();
            var coverLinkFile = new KifaFile(coverLink);
            var coverFile = file.Parent.GetFile($"{file.BaseName}.{coverLinkFile.Extension}");
            coverFile.Delete();
            coverLinkFile.Copy(coverFile);
            return coverFile;
        });
}
