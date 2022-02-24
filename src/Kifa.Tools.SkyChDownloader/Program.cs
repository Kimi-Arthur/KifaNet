using CommandLine;
using Kifa.Tools.SkyChDownloader.Commands;

namespace Kifa.Tools.SkyChDownloader; 

class Program {
    static int Main(string[] args) =>
        KifaCommand.Run(parameters => Parser.Default.ParseArguments(parameters, typeof(DownloadLiveCommand)), args);
}