using System.Linq;
using CommandLine;
using Kifa.Bilibili;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("bangumi", HelpText = "Download all high quality Bilibili videos for one bangumi.")]
public class DownloadBangumiCommand : DownloadCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Bangumi ID. Should start with 'md' or 'ss'.")]
    public string BangumiId { get; set; }

    public override int Execute() {
        var bangumi = BilibiliBangumi.Client.Get(BangumiId);
        if (bangumi == null) {
            logger.Fatal($"Cannot find Bangumi ({BangumiId}). Exiting.");
            return 1;
        }

        foreach (var videoId in bangumi.Aids.Distinct()) {
            var video = BilibiliVideo.Client.Get(videoId);
            if (video == null) {
                logger.Error($"Cannot find video ({videoId}). Skipping.");
                continue;
            }

            foreach (var page in video.Pages) {
                Download(video, page.Id, $"{bangumi.Title}-{bangumi.Id}");
            }
        }

        return 0;
    }
}
