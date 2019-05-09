using System;
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
            var uploader = PimixService.Get<BilibiliUploader>(UploaderId);
            foreach (var videoId in uploader.Aids) {
                PimixService.Create(new BilibiliVideo {
                    Id = videoId
                });
                var video = PimixService.Get<BilibiliVideo>(videoId);
                foreach (var page in video.Pages) {
                    var (length, stream) = video.DownloadVideo(page.Id, SourceChoice);
                    if (length == null) {
                        continue;
                    }

                    var targetFile = CurrentFolder.GetFile($"{video.GetDesiredName(page.Id)}.mp4");
                    if (targetFile.Exists()) {
                        if (targetFile.Length() == length) {
                            logger.Info($"Target file {targetFile} already exists. Skipped.");
                            continue;
                        }

                        logger.Info($"Target file {targetFile} exists, " +
                                    $"but size ({targetFile.Length()}) is different from source ({length}). " +
                                    "Will be removed.");
                    }

                    try {
                        logger.Info($"Start downloading video to {targetFile}");
                        targetFile.Delete();
                        targetFile.Write(stream);
                        logger.Info($"Successfullly downloaded video to {targetFile}");
                    } catch (Exception ex) {
                        logger.Warn(ex, $"Failed to download {targetFile}");
                    }
                }
            }

            return 0;
        }
    }
}
