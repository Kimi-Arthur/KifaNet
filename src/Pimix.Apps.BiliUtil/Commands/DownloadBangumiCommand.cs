using System;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Bilibili;
using Pimix.Service;

namespace Pimix.Apps.BiliUtil.Commands {
    [Verb("bangumi", HelpText = "Download all high quality Bilibili videos for one bangumi.")]
    public class DownloadBangumiCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Bangumi ID.")]
        public string BangumiId { get; set; }

        [Option('s', "source", HelpText = "Override default source choice.")]
        public int SourceChoice { get; set; } = BilibiliVideo.DefaultBiliplusSourceChoice;

        public override int Execute() {
            BilibiliBangumi.Client.Set(new BilibiliBangumi {
                Id = BangumiId
            });
            var bangumi = BilibiliBangumi.Client.Get(BangumiId);
            foreach (var videoId in bangumi.Aids.Distinct()) {
                BilibiliVideo.Client.Set(new BilibiliVideo {
                    Id = videoId
                });
                var video = BilibiliVideo.Client.Get(videoId);
                foreach (var page in video.Pages) {
                    var targetFile =
                        CurrentFolder.GetFile(
                            $"{video.GetDesiredName(page.Id, extraPath: $"{bangumi.Name}-{bangumi.Id}")}.mp4");
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
