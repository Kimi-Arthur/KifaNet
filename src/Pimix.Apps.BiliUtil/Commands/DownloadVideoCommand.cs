using CommandLine;

namespace Pimix.Apps.BiliUtil.Commands {
    [Verb("video", HelpText = "Download high quality Bilibili videos from biliplus.")]
    public class DownloadVideoCommand : PimixCommand {
        [Value(0, Required = true,
            HelpText = "The video id from Bilibili. With possible p{n} as a suffix.")]
        public string Aid { get; set; }

        public override int Execute() {
            return -1;
        }
    }
}
