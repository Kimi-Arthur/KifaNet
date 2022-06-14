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
        BilibiliBangumi.Client.Set(new BilibiliBangumi {
            Id = BangumiId
        });
        var bangumi = BilibiliBangumi.Client.Get(BangumiId);
        foreach (var videoId in bangumi.Aids.Distinct()) {
            BilibiliVideo.Client.Set(new BilibiliVideo {
                Id = videoId
            });
            var video = BilibiliVideo.Client.Get(videoId);
            foreach (var page in video.Pages) {
                DownloadPart(video, page.Id, $"{bangumi.Title}-{bangumi.Id}");
            }
        }

        return 0;
    }
}
