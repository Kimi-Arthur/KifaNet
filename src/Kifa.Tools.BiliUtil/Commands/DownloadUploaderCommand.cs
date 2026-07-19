using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Bilibili;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("up", HelpText = "Download all high quality Bilibili videos for one uploader.")]
public class DownloadUploaderCommand : DownloadCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Uploader ID.")]
    public string UploaderId { get; set; }

    [Option('f', "folder",
        HelpText =
            "Extra inner folder name for the group of videos (especially if subset of videos are selected).")]
    public string? InnerFolder { get; set; }

    [Option('l', "oldest-first", HelpText = "Download oldest video first.")]
    public bool OldestFirst { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var uploader = BilibiliUploader.Client.Get(UploaderId);
        if (uploader == null) {
            Logger.Fatal($"Cannot find uploader ({UploaderId}). Exiting.");
            return 1;
        }

        var videos = SelectMany(
            BilibiliVideo.Client
                .Get(OldestFirst ? uploader.Aids : Enumerable.Reverse(uploader.Aids).ToList())
                .ExceptNull().ToList(),
            video => $"{video.Id} {video.Title} ({video.Pages.Count})", "videos to download");
        if (videos.Status == KifaActionStatus.OK) {
            DownloadVideos(uploader, videos.Value);
        }

        var deletedVideos = SelectMany(
            BilibiliVideo.Client
                .Get(OldestFirst
                    ? uploader.RemovedAids
                    : Enumerable.Reverse(uploader.RemovedAids).ToList()).ExceptNull().ToList(),
            video => $"{video.Id} {video.Title} ({video.Pages.Count})",
            "deleted videos to download");
        if (deletedVideos.Status == KifaActionStatus.OK) {
            DownloadVideos(uploader, deletedVideos.Value);
        }

        return LogSummary();
    }

    void DownloadVideos(BilibiliUploader uploader, List<BilibiliVideo> videos) {
        foreach (var video in videos) {
            foreach (var page in video.Pages) {
                ExecuteItem($"{video.Id}p{page.Id} {video.Title} {page.Title}",
                    () => Download(video, page.Id, uploader: uploader, extraFolder: InnerFolder));
            }
        }
    }
}
