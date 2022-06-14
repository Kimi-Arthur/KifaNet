using System.Linq;
using CommandLine;
using Kifa.Bilibili;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("up", HelpText = "Download all high quality Bilibili videos for one uploader.")]
public class DownloadUploaderCommand : DownloadCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Uploader ID.")]
    public string UploaderId { get; set; }

    public override int Execute() {
        var uploader = BilibiliUploader.Client.Get(UploaderId);
        foreach (var videoId in Enumerable.Reverse(uploader.Aids)) {
            var video = BilibiliVideo.Client.Get(videoId);
            foreach (var page in video.Pages) {
                video.DownloadPart(page.Id, DownloadOptions, uploader: uploader);
            }
        }

        return 0;
    }
}
