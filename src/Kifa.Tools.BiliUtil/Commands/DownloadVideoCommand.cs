using System.Linq;
using CommandLine;
using Kifa.Bilibili;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("video", HelpText = "Download high quality Bilibili videos from biliplus.")]
public class DownloadVideoCommand : DownloadCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true,
        HelpText = "The video id from Bilibili. With possible p{n} as a suffix.")]
    public string Aid { get; set; }

    public override int Execute() {
        var segments = Aid.Split('p');
        var aid = segments.First();

        var video = BilibiliVideo.Client.Get(aid);
        if (video == null) {
            Logger.Fatal($"Cannot find video ({aid}). Exiting.");
            return 1;
        }

        if (segments.Length == 2) {
            var pid = int.Parse(segments.Last());
            Logger.Info($"Downloading part {pid} of video {video.Id}...");
            Download(video, pid);
            return 0;
        }

        Logger.Info($"Downloading all parts of video {video.Id}...");
        foreach (var page in video.Pages) {
            Download(video, page.Id);
        }

        return 0;
    }
}
