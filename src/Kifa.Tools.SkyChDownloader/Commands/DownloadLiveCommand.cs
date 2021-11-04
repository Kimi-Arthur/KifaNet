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

            logger.Info(skyProgram.GetVideoLink());
            return 0;
        }
    }
}
