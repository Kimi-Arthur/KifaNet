using System.Linq;
using CommandLine;
using Kifa.Bilibili;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("bangumi", HelpText = "Download all high quality Bilibili videos for one bangumi.")]
public class DownloadBangumiCommand : DownloadCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Bangumi ID. Should start with 'md' or 'ss'.")]
    public string BangumiId {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    [Option('e', "include-extras", HelpText = "Include extra video files.")]
    public bool IncludeExtras { get; set; } = false;

    [Option('r', "region",
        HelpText = "Region of the video(s). Possible values: cn, hk, any. Default is cn.")]
    public override string Region { get; set; } = "cn";

    public override int Execute(KifaTask? task = null) {
        var bangumi = BilibiliBangumi.Client.Get(BangumiId);
        if (bangumi == null) {
            Logger.Fatal($"Cannot find Bangumi ({BangumiId}). Exiting.");
            return 1;
        }

        foreach (var videoId in bangumi.Aids.Distinct()) {
            ExecuteItem(videoId, () => DownloadVideo(bangumi, videoId, extraFolder: null));
        }

        if (IncludeExtras) {
            Logger.Info("Download extra video files.");

            foreach (var videoId in bangumi.ExtraAids.Distinct()) {
                ExecuteItem(videoId, () => DownloadVideo(bangumi, videoId, extraFolder: "Extras"));
            }
        }

        return LogSummary();
    }

    KifaActionResult DownloadVideo(BilibiliBangumi bangumi, string videoId, string? extraFolder) {
        var video = BilibiliVideo.Client.Get(videoId);
        if (video?.Pages == null) {
            return KifaActionResult.Error($"Cannot find video ({videoId}).");
        }

        var results = new KifaBatchActionResult();
        foreach (var page in video.Pages) {
            results.Add($"{video.Id}p{page.Id} {video.Title} {page.Title}",
                KifaActionResult.FromAction(() => Download(video, page.Id,
                    alternativeFolder: $"{bangumi.Title}.{bangumi.Id}",
                    extraFolder: extraFolder)));
        }

        return results;
    }
}
