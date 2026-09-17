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
        var uploader = YouTubeUploader.Resolve(UploaderId, refresh: Refresh);
        if (uploader == null) {
            Logger.Fatal($"Cannot find uploader ({UploaderId}). Exiting.");
            return 1;
        }

        var uploaderVideos = YouTubeUploaderVideos.Client.Get(uploader.Id, new() {
            Refresh = Refresh
        });
        if (uploaderVideos == null) {
            Logger.Fatal($"Cannot find video list for uploader ({UploaderId}). Exiting.");
            return 1;
        }

        var sections = new List<List<string>> {
            uploaderVideos.Videos,
            uploaderVideos.Shorts,
            uploaderVideos.Streams
        };

        foreach (var section in sections) {
            var videosToDownload = OldestFirst
                ? section.AsEnumerable().Reverse().ToList()
                : section;

            foreach (var videoId in videosToDownload) {
                ExecuteItem(videoId, () => DownloadVideo(uploader, videoId));
                if (BreakOnExisting && LastItemAlreadyExists) {
                    Logger.Info($"Stopping early: video ({videoId}) already exists in section.");
                    break;
                }
            }
        }

        return LogSummary();
    }

    KifaActionResult DownloadVideo(YouTubeUploader uploader, string videoId) {
        var video = YouTubeVideo.Client.Get(videoId, new() {
            Refresh = Refresh
        });
        if (video == null) {
            LastItemAlreadyExists = false;
            return KifaActionResult.Error($"Cannot find video ({videoId}).");
        }

        return KifaActionResult.FromAction(() => Download(video, extraFolder: InnerFolder, uploader: uploader));
    }
}
