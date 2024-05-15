using System.Linq;
using CommandLine;
using Kifa.Bilibili;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("bangumi", HelpText = "Download all high quality Bilibili videos for one bangumi.")]
public class DownloadBangumiCommand : DownloadCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Bangumi ID. Should start with 'md' or 'ss'.")]
    public string BangumiId {
        get => Late.Get(bangumiId);
        set => Late.Set(ref bangumiId, value);
    }

    [Option('e', "include-extras", HelpText = "Include extra video files.")]
    public bool IncludeExtras { get; set; } = false;

    string? bangumiId;

    public override int Execute() {
        var bangumi = BilibiliBangumi.Client.Get(BangumiId);
        if (bangumi == null) {
            Logger.Fatal($"Cannot find Bangumi ({BangumiId}). Exiting.");
            return 1;
        }

        foreach (var videoId in bangumi.Aids.Distinct()) {
            var video = BilibiliVideo.Client.Get(videoId);
            if (video == null) {
                Logger.Error($"Cannot find video ({videoId}). Skipping.");
                continue;
            }

            foreach (var page in video.Pages) {
                ExecuteItem($"{video.Id}p{page.Id} {video.Title} {page.Title}",
                    () => Download(video, page.Id, $"{bangumi.Title}-{bangumi.Id}"));
            }
        }

        if (IncludeExtras) {
            Logger.Info("Download extra video files.");

            foreach (var videoId in bangumi.ExtraAids.Distinct()) {
                var video = BilibiliVideo.Client.Get(videoId);
                if (video == null) {
                    Logger.Error($"Cannot find video ({videoId}). Skipping.");
                    continue;
                }

                foreach (var page in video.Pages) {
                    ExecuteItem($"{video.Id}p{page.Id} {video.Title} {page.Title}",
                        () => Download(video, page.Id, $"{bangumi.Title}-{bangumi.Id}/Extras"));
                }
            }
        }

        LogSummary();
        return 0;
    }
}
