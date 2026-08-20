using System.Collections.Generic;
using CommandLine;
using Kifa.Jobs;
using Kifa.Service;
using Kifa.YouTube;
using NLog;

namespace Kifa.Tools.YoutubeUtil.Commands;

[Verb("video", HelpText = "Download YouTube video.")]
public class DownloadVideoCommand : DownloadCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Video ids from YouTube.")]
    public IEnumerable<string> Ids { get; set; } = [];

    [Option('n', "use-video-name",
        HelpText =
            "Use video name (and id) as folder name instead of uploader name. This is best for a collection of videos with one id.")]
    public bool UseVideoNameFolder { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        foreach (var id in Ids) {
            ExecuteItem(id, () => DownloadVideo(id));
        }

        return LogSummary();
    }

    KifaActionResult DownloadVideo(string id) {
        var video = YouTubeVideo.Client.Get(id);
        if (video == null) {
            return KifaActionResult.Error($"Cannot find video ({id}).");
        }

        return KifaActionResult.FromAction(() => Download(video,
            alternativeFolder: UseVideoNameFolder ? $"{video.Title}.{video.Id}" : null));
    }
}
