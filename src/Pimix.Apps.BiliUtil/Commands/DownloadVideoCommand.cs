using System.Linq;
using CommandLine;
using NLog;
using Pimix.Bilibili;
using Pimix.Service;

namespace Pimix.Apps.BiliUtil.Commands {
    [Verb("video", HelpText = "Download high quality Bilibili videos from biliplus.")]
    public class DownloadVideoCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true,
            HelpText = "The video id from Bilibili. With possible p{n} as a suffix.")]
        public string Aid { get; set; }

        [Option('s', "source", HelpText = "Override default source choice.")]
        public int SourceChoice { get; set; } = BilibiliVideo.DefaultBiliplusSourceChoice;

        public override int Execute() {
            var segments = Aid.Split('p');
            var aid = segments.First();
            var pid = segments.Length == 2 ? int.Parse(segments.Last()) : 1;

            PimixService.Update(new BilibiliVideo {Id = aid});
            var video = PimixService.Get<BilibiliVideo>(aid);

            var (length, stream) = video.DownloadVideo(pid, SourceChoice);
            logger.Error(length);
            if (length == null) {
                return -1;
            }

            var targetFile = CurrentFolder.GetFile($"{video.GetDesiredName(pid)}.mp4");
            if (targetFile.Length() == length) {
                logger.Info($"Target file {targetFile} already exists. Skipped.");
                return 0;
            }

            targetFile.Delete();
            targetFile.Write(stream);
            logger.Info($"Successfullly downloaded video to {targetFile}");

            return 0;
        }
    }
}
