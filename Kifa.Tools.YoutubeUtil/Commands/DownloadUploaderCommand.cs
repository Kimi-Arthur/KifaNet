using CommandLine;
using Kifa.Jobs;
using Kifa.Service;
using Kifa.YouTube;
using NLog;

namespace Kifa.Tools.YoutubeUtil.Commands;

[Verb("up", HelpText = "Download all high quality YouTube videos for one uploader.")]
public class DownloadUploaderCommand : DownloadCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Uploader ID or channel handle.")]
    public string UploaderId { get; set; } = "";

    [Option('f', "folder", HelpText = "Extra inner folder name for the group of videos.")]
    public string? InnerFolder { get; set; }

    [Option('l', "oldest-first", HelpText = "Download oldest video first.")]
    public bool OldestFirst { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var uploader = YouTubeUploader.Client.Get(UploaderId);
        if (uploader == null) {
            Logger.Fatal($"Cannot find uploader ({UploaderId}). Exiting.");
            return 1;
        }

        var videosToDownload = OldestFirst
            ? uploader.Videos.AsEnumerable().Reverse().ToList()
            : uploader.Videos;

        foreach (var videoId in videosToDownload) {
            var video = YouTubeVideo.Client.Get(videoId);
            if (video == null) {
                ExecuteItem(videoId,
                    () => KifaActionResult.Error($"Cannot find video ({videoId})."));
                continue;
            }

            ExecuteItem($"{video.Id} {video.Title}",
                () => Download(video, alternativeFolder: InnerFolder));
        }

        return LogSummary();
    }
}
