using CommandLine;
using Pimix.Apps.BiliUtil.Commands;

namespace Pimix.Apps.BiliUtil {
    class Program {
        static int Main(string[] args) =>
            PimixCommand.Run(
                Parser.Default
                    .ParseArguments<GetChatCommand, RenameVideoCommand, LinkVideoCommand, DownloadVideoCommand,
                        DownloadUploaderCommand, DownloadBangumiCommand, MergeCommand>, args);
    }
}
