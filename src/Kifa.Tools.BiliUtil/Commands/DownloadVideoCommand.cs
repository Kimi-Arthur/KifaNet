using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Bilibili;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("video", HelpText = "Download high quality Bilibili videos from biliplus.")]
public class DownloadVideoCommand : DownloadCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true,
        HelpText = "Video ids from Bilibili, like av12345, with possible p{n} as a suffix.")]
    public IEnumerable<string> Aids { get; set; }

    public override int Execute() {
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
                Download(video, pid);
                continue;
            }

            Logger.Info($"Downloading all parts of video {video.Id}...");
            foreach (var page in video.Pages) {
                Download(video, page.Id);
            }
        }

        return 0;
    }
}
