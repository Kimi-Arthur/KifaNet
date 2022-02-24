using System.Linq;
using CommandLine;
using Kifa.Bilibili;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("up", HelpText = "Download all high quality Bilibili videos for one uploader.")]
public class DownloadUploaderCommand : KifaCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Uploader ID.")]
    public string UploaderId { get; set; }

    [Option('d', "prefix-date", HelpText = "Prefix file name with the upload date.")]
    public bool PrefixDate { get; set; } = false;

    [Option('a', "all-staff", HelpText = "Put video in all folders owned by any staff.")]
    public bool AllStaff { get; set; } = false;

    [Option('s', "source", HelpText = "Override default source choice.")]
    public int SourceChoice { get; set; } = BilibiliVideo.DefaultBiliplusSourceChoice;

    public override int Execute() {
        var uploader = BilibiliUploader.Client.Get(UploaderId);
        foreach (var videoId in Enumerable.Reverse(uploader.Aids)) {
            var video = BilibiliVideo.Client.Get(videoId);
            foreach (var page in video.Pages) {
                video.DownloadPart(page.Id, SourceChoice, CurrentFolder, prefixDate: PrefixDate,
                    uploader: uploader);
            }
        }

        return 0;
    }
}
