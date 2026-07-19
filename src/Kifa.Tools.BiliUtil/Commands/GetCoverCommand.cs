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

[Verb("cover", HelpText = "Get Bilibili cover along side existing video files.")]
public class GetCoverCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target files to download cover images for.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute(KifaTask? task = null) {
        var foundFiles = KifaFile.FindExistingFiles(FileNames, pattern: "*.mp4");
        var selectedFiles = SelectMany(foundFiles, file => file.ToString(),
            "files to download cover images for");

        if (selectedFiles.Status != KifaActionStatus.OK) {
            ExecuteItem("files to download cover images for", () => selectedFiles);
            return LogSummary();
        }

        foreach (var file in selectedFiles.Value) {
            ExecuteItem(file.ToString(), () => GetCover(file));
        }

        return LogSummary();
    }

    static readonly List<string> ExpectedCoverExtensions = new() {
        "jpg",
        "png"
    };

    static KifaActionResult GetCover(KifaFile file)
        => KifaActionResult.FromAction(() => {
            var foundCoverExtension = ExpectedCoverExtensions.FirstOrDefault(ext
                => file.Parent.GetFile($"{file.BaseName}.{ext}").Exists());
            if (foundCoverExtension != null) {
                var foundCoverFile = file.Parent.GetFile($"{file.BaseName}.{foundCoverExtension}");
                Logger.Info($"Found cover file {foundCoverFile} for {file}. Skipped.");
                return;
            }

            var video = BilibiliVideo.Parse(file.Id);
            if (video.video == null) {
                throw new Exception($"Video not found for {file.Id}.");
            }

            var coverLink = video.video.Cover.ToString();
            var coverLinkFile = new KifaFile(coverLink);
            var coverFile = file.Parent.GetFile($"{file.BaseName}.{coverLinkFile.Extension}");
            coverFile.Delete();
            coverLinkFile.Copy(coverFile);
        });
}
