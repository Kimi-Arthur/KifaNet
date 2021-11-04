using System.Web;
using CommandLine;
using Kifa.Service;
using Kifa.SkyCh;
using NLog;

namespace Kifa.Tools.SkyChDownloader.Commands {
    [Verb("live", HelpText = "Download program with id from Live TV page.")]
    public class DownloadLiveCommand : KifaCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Live TV ID.")]
        public string LiveId { get; set; }

        public override int Execute() {
            var skyProgram = new KifaServiceRestClient<SkyProgram>().Get(LiveId);

            var title = HttpUtility.HtmlDecode(skyProgram.Title);
            if (skyProgram.Subtitle?.Length > 0) {
                title += " - " + HttpUtility.HtmlDecode(skyProgram.Subtitle);
            }

            logger.Info($"Name: {title}.{skyProgram.Id}.mp4");
            logger.Info($"Cover: {skyProgram.ImageLink}");
            logger.Info($"Link: {skyProgram.GetVideoLink()}");
            return 0;
        }
    }
}
