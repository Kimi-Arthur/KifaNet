using Kifa.Tools.SkyChDownloader.Commands;

namespace Kifa.Tools.SkyChDownloader;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(args, typeof(DownloadLiveCommand),
            typeof(DownloadProgramCommand));
}
