using System;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Bilibili;
using Pimix.Service;

namespace Pimix.Apps.BiliUtil.Commands {
    [Verb("up", HelpText = "Download all high quality Bilibili videos for one uploader.")]
    public class DownloadUploaderCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true,
            HelpText = "Uploader ID.")]
        public string UploaderId { get; set; }

        [Option('s', "source", HelpText = "Override default source choice.")]
        public int SourceChoice { get; set; } = BilibiliVideo.DefaultBiliplusSourceChoice;

        public override int Execute() {
            PimixService.Create(new BilibiliUploader {
                Id = UploaderId
            });
            var uploader = BilibiliUploader.Client.Get(UploaderId);
            foreach (var videoId in uploader.Aids.Distinct()) {
                PimixService.Create(new BilibiliVideo {
                    Id = videoId
                });
                var video = BilibiliVideo.Client.Get(videoId);
                foreach (var page in video.Pages) {
                    var targetFile = CurrentFolder.GetFile($"{video.GetDesiredName(page.Id)}.mp4");
                    try {
                        targetFile.WriteIfNotFinished(() => video.DownloadVideo(page.Id, SourceChoice));
                    } catch (Exception e) {
                        logger.Warn(e, $"Failed to download {targetFile}.");
                    }
                }
            }

            return 0;
        }
    }
}
