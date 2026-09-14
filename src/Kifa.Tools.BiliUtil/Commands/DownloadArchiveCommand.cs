using CommandLine;
using Kifa.Bilibili;
using Kifa.Jobs;
using Kifa.Service;
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
            ExecuteItem(videoId, () => DownloadVideo(archive, videoId));
            if (BreakOnExisting && LastItemAlreadyExists) {
                Logger.Info($"Stopping early: video ({videoId}) already exists.");
                break;
            }
        }

        return LogSummary();
    }

    KifaActionResult DownloadVideo(BilibiliArchive archive, string videoId) {
        var video = BilibiliVideo.Client.Get(videoId);
        if (video?.Pages == null) {
            LastItemAlreadyExists = false;
            return KifaActionResult.Error($"Cannot find video ({videoId}).");
        }

        var allPagesExisted = true;
        var results = new KifaBatchActionResult();
        foreach (var page in video.Pages) {
            results.Add($"{video.Id}p{page.Id} {video.Title} {page.Title}",
                KifaActionResult.FromAction(() => {
                    var result = Download(video, page.Id,
                        extraFolder: archive.GetArchiveFolder());
                    if (!LastItemAlreadyExists) {
                        allPagesExisted = false;
                    }

                    return result;
                }));
        }

        LastItemAlreadyExists = allPagesExisted && video.Pages.Count > 0;
        return results;
    }
}
