using CommandLine;
using Kifa.Tools.YoutubeUtil.Commands;

namespace Kifa.Tools.YoutubeUtil;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(
            parameters => Parser.Default.ParseArguments(parameters, typeof(DownloadVideoCommand)),
            args);
}
