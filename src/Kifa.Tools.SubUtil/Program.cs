using CommandLine;
using Kifa.Tools.SubUtil.Commands;

namespace Kifa.Tools.SubUtil;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(
            Parser.Default
                .ParseArguments<GenerateCommand, UpdateCommand,
                    ImportCommand, ExtractCommand, SyncCommand, DownloadSubcatCommand, MoveCommand>, args);
}
