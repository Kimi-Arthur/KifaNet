using System.Linq;
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

        var videos = SelectMany(BilibiliVideo.Client.Get(archive.Videos).ExceptNull().ToList(),
            v => FormatVideoChoice(v), "videos to download");
        if (videos.Status == KifaActionStatus.OK && videos.Value != null) {
            foreach (var video in videos.Value) {
                ExecuteItem(video.Id, () => DownloadVideo(archive, video));
                if (BreakOnExisting && LastItemAlreadyExists) {
                    Logger.Info($"Stopping early: video ({video.Id}) already exists.");
                    break;
                }
            }
        }

        return LogSummary();
    }

    KifaActionResult DownloadVideo(BilibiliArchive archive, BilibiliVideo video) {
        if (video.Pages == null || video.Pages.Count == 0) {
            LastItemAlreadyExists = false;
            return KifaActionResult.Error($"No pages found for video ({video.Id}).");
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
