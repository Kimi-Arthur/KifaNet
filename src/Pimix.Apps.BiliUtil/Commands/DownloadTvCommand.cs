using System;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Bilibili;
using Pimix.Service;

namespace Pimix.Apps.BiliUtil.Commands {
    [Verb("tv", HelpText = "Download all high quality Bilibili videos for one TV show.")]
    public class DownloadTvCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "TV Show ID.")]
        public string TvShowId { get; set; }

        [Option('s', "source", HelpText = "Override default source choice.")]
        public int SourceChoice { get; set; } = BilibiliVideo.DefaultBiliplusSourceChoice;

        public override int Execute() {
            PimixService.Create(new BilibiliTv {
                Id = TvShowId
            });
            var tv = PimixService.Get<BilibiliTv>(TvShowId);
            foreach (var videoId in tv.Aids.Distinct()) {
                PimixService.Create(new BilibiliVideo {
                    Id = videoId
                });
                var video = PimixService.Get<BilibiliVideo>(videoId);
                foreach (var page in video.Pages) {
                    var targetFile =
                        CurrentFolder.GetFile($"{video.GetDesiredName(page.Id, extraPath: $"{tv.Name}-{tv.Id}")}.mp4");
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