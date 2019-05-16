using System;
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
            var pid = segments.Length == 2 ? int.Parse(segments.Last()) : 0;

            PimixService.Create(new BilibiliVideo {
                Id = aid
            });

            var video = BilibiliVideo.Client.Get(aid);

            if (pid > 0) {
                var targetFile = CurrentFolder.GetFile($"{video.GetDesiredName(pid)}.mp4");
                try {
                    targetFile.WriteIfNotFinished(() => video.DownloadVideo(pid, SourceChoice));
                } catch (Exception e) {
                    logger.Warn(e, $"Failed to download {targetFile}.");
                }

                return 0;
            }

            foreach (var page in video.Pages) {
                var targetFile =
                    CurrentFolder.GetFile($"{video.GetDesiredName(page.Id)}.mp4");
                try {
                    targetFile.WriteIfNotFinished(() => video.DownloadVideo(page.Id, SourceChoice));
                } catch (Exception e) {
                    logger.Warn(e, $"Failed to download {targetFile}.");
                }
            }

            return 0;
        }
    }
}
