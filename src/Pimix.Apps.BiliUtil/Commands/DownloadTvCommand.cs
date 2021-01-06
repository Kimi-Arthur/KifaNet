using System.Linq;
using CommandLine;
using NLog;
using Kifa.Bilibili;

namespace Pimix.Apps.BiliUtil.Commands {
    [Verb("tv", HelpText = "Download all high quality Bilibili videos for one TV show.")]
    public class DownloadTvCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "TV Show ID.")]
        public string TvShowId { get; set; }

        [Option('s', "source", HelpText = "Override default source choice.")]
        public int SourceChoice { get; set; } = BilibiliVideo.DefaultBiliplusSourceChoice;

        public override int Execute() {
            BilibiliTv.Client.Set(new BilibiliTv {
                Id = TvShowId
            });
            var tv = BilibiliTv.Client.Get(TvShowId);
            foreach (var videoId in tv.Aids.Distinct()) {
                BilibiliVideo.Client.Set(new BilibiliVideo {
                    Id = videoId
                });
                var video = BilibiliVideo.Client.Get(videoId);
                foreach (var page in video.Pages) {
                    video.DownloadPart(page.Id, SourceChoice, CurrentFolder, $"{tv.Name}-{tv.Id}");
                }
            }

            return 0;
        }
    }
}
