using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Bilibili;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("video", HelpText = "Download high quality Bilibili videos from biliplus.")]
public class DownloadVideoCommand : DownloadCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true,
        HelpText = "Video ids from Bilibili, like av12345, with possible p{n} as a suffix.")]
    public IEnumerable<string> Aids { get; set; }

    [Option('n', "use-video-name",
        HelpText =
            "Use video name (and id) as folder name instead of uploader name. This is best for a collection of videos with one id.")]
    public bool UseVideoNameFolder { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        foreach (var aidWithPage in Aids) {
            ExecuteItem(aidWithPage, () => DownloadVideo(aidWithPage));
            if (BreakOnExisting && LastItemAlreadyExists) {
                Logger.Info($"Stopping early: video ({aidWithPage}) already exists.");
                break;
            }
        }

        return LogSummary();
    }

    KifaActionResult DownloadVideo(string aidWithPage) {
        var segments = aidWithPage.Split('p');
        var aid = segments.First();

        var video = BilibiliVideo.Client.Get(aid);
        if (video == null) {
            LastItemAlreadyExists = false;
            return KifaActionResult.Error($"Cannot find video ({aid}).");
        }

        if (segments.Length == 2) {
            var pid = int.Parse(segments.Last());
            return KifaActionResult.FromAction(() => Download(video, pid,
                alternativeFolder: UseVideoNameFolder ? $"{video.Title}.{video.Id}" : null));
        }

        var allPagesExisted = true;
        var results = new KifaBatchActionResult();
        foreach (var page in video.Pages) {
            results.Add($"{video.Id}p{page.Id} {video.Title} {page.Title}",
                KifaActionResult.FromAction(() => {
                    var result = Download(video, page.Id,
                        alternativeFolder: UseVideoNameFolder ? $"{video.Title}.{video.Id}" : null);
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
