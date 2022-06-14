using System.Linq;
using CommandLine;
using Kifa.Bilibili;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("video", HelpText = "Download high quality Bilibili videos from biliplus.")]
public class DownloadVideoCommand : DownloadCommand {
    [Value(0, Required = true,
        HelpText = "The video id from Bilibili. With possible p{n} as a suffix.")]
    public string Aid { get; set; }

    public override int Execute() {
        var segments = Aid.Split('p');
        var aid = segments.First();
        var pid = segments.Length == 2 ? int.Parse(segments.Last()) : 0;

        BilibiliVideo.Client.Set(new BilibiliVideo {
            Id = aid
        });

        var video = BilibiliVideo.Client.Get(aid);

        if (pid > 0) {
            DownloadPart(video, pid);
            return 0;
        }

        foreach (var page in video.Pages) {
            DownloadPart(video, page.Id);
        }

        return 0;
    }
}
