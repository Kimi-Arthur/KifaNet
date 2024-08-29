using System.Linq;
using CommandLine;
using Kifa.Bilibili;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("up", HelpText = "Download all high quality Bilibili videos for one uploader.")]
public class DownloadUploaderCommand : DownloadCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Uploader ID.")]
    public string UploaderId { get; set; }

    public override int Execute(KifaTask? task = null) {
        var uploader = BilibiliUploader.Client.Get(UploaderId);
        if (uploader == null) {
            Logger.Fatal($"Cannot find uploader ({UploaderId}). Exiting.");
            return 1;
        }

        foreach (var videoId in Enumerable.Reverse(uploader.Aids)) {
            var video = BilibiliVideo.Client.Get(videoId);
            if (video == null) {
                Logger.Error($"Cannot find video ({videoId}). Skipping.");
                continue;
            }

            foreach (var page in video.Pages) {
                Download(video, page.Id, uploader: uploader);
            }
        }

        return 0;
    }
}
