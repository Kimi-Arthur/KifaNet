using System.Linq;
using CommandLine;
using Kifa.Bilibili;

namespace Kifa.Tools.BiliUtil.Commands {
    [Verb("video", HelpText = "Download high quality Bilibili videos from biliplus.")]
    public class DownloadVideoCommand : PimixCommand {
        [Value(0, Required = true,
            HelpText = "The video id from Bilibili. With possible p{n} as a suffix.")]
        public string Aid { get; set; }

        [Option('d', "prefix-date", HelpText = "Prefix file name with the upload date.")]
        public bool PrefixDate { get; set; } = false;

        [Option('s', "source", HelpText = "Override default source choice.")]
        public int SourceChoice { get; set; } = BilibiliVideo.DefaultBiliplusSourceChoice;

        public override int Execute() {
            var segments = Aid.Split('p');
            var aid = segments.First();
            var pid = segments.Length == 2 ? int.Parse(segments.Last()) : 0;

            BilibiliVideo.Client.Set(new BilibiliVideo {
                Id = aid
            });

            var video = BilibiliVideo.Client.Get(aid);

            if (pid > 0) {
                video.DownloadPart(pid, SourceChoice, CurrentFolder);
                return 0;
            }

            foreach (var page in video.Pages) {
                video.DownloadPart(page.Id, SourceChoice, CurrentFolder, prefixDate: PrefixDate);
            }

            return 0;
        }
    }
}
