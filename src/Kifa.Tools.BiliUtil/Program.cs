using CommandLine;
using Kifa.Tools.BiliUtil.Commands;

namespace Kifa.Tools.BiliUtil; 

class Program {
    static int Main(string[] args) =>
        KifaCommand.Run(
            Parser.Default
                .ParseArguments<GetChatCommand, RenameVideoCommand, LinkVideoCommand, DownloadVideoCommand,
                    DownloadUploaderCommand, DownloadBangumiCommand, MergeCommand>, args);
}