using CommandLine;
using Kifa.Bilibili;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("archive", HelpText = "Download all high quality Bilibili videos for one archive.")]
public class DownloadArchiveCommand : DownloadCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true,
        HelpText = "Archive ID with author and season id separated with slash '/'.")]
    public string ArchiveId { get; set; }

    public override int Execute(KifaTask? task = null) {
        var archive = BilibiliArchive.Client.Get(ArchiveId);
        if (archive == null) {
            Logger.Fatal($"Cannot find archive ({ArchiveId}). Exiting.");
            return 1;
        }

        foreach (var videoId in archive.Videos) {
            var video = BilibiliVideo.Client.Get(videoId);
            if (video == null) {
                Logger.Error($"Cannot find video ({videoId}). Skipping.");
                continue;
            }

            Logger.Trace($"To download video {video}");

            foreach (var page in video.Pages) {
                Download(video, page.Id, alternativeFolder: archive.GetBaseFolder());
            }
        }

        return 0;
    }
}
