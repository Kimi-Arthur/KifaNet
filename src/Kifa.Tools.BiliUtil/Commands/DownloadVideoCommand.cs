using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Bilibili;
using Kifa.Jobs;
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
            var segments = aidWithPage.Split('p');
            var aid = segments.First();

            var video = BilibiliVideo.Client.Get(aid);
            if (video == null) {
                Logger.Fatal($"Cannot find video ({aid}). Exiting.");
                continue;
            }

            if (segments.Length == 2) {
                var pid = int.Parse(segments.Last());
                Logger.Info($"Downloading part {pid} of video {video.Id}...");
                Download(video, pid,
                    alternativeFolder: UseVideoNameFolder ? $"{video.Title}.{video.Id}" : null);
                continue;
            }

            Logger.Info($"Downloading all parts of video {video.Id}...");
            foreach (var page in video.Pages) {
                Download(video, page.Id,
                    alternativeFolder: UseVideoNameFolder ? $"{video.Title}.{video.Id}" : null);
            }
        }

        return 0;
    }
}
