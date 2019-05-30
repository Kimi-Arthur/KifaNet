using CommandLine;
using Pimix.Apps.FileUtil.Commands;

namespace Pimix.Apps.FileUtil {
    class Program {
        static int Main(string[] args)
            => PimixCommand.Run(Parser.Default
                .ParseArguments<
                    InfoCommand,
                    CopyCommand,
                    VerifyCommand,
                    RemoveCommand,
                    MoveCommand,
                    LinkCommand,
                    ListCommand,
                    UploadCommand,
                    AddCommand,
                    GetCommand,
                    TouchCommand,
                    CleanCommand,
                    ImportCommand
                >, args);
    }
}
