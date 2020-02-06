using CommandLine;
using Pimix.Apps.FileUtil.Commands;

namespace Pimix.Apps.FileUtil {
    class Program {
        static int Main(string[] args)
            => PimixCommand.Run(Parser.Default
                .ParseArguments<
                    CheckCommand,
                    _InfoCommand,
                    _CopyCommand,
                    _VerifyCommand,
                    RemoveCommand,
                    LinkCommand,
                    ListCommand,
                    UploadCommand,
                    AddCommand,
                    GetCommand,
                    TouchCommand,
                    CleanCommand,
                    ImportCommand,
                    TrashCommand,
                    RemoveEmptyCommand,
                    DecodeCommand
                >, args);
    }
}
