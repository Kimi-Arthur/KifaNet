using CommandLine;
using Pimix.Apps.FileUtil.Commands;

namespace Pimix.Apps.FileUtil {
    class Program {
        static int Main(string[] args)
            => PimixCommand.Run(Parser.Default
                .ParseArguments<
                    CheckCommand,
                    _InfoCommand,
                    _VerifyCommand,
                    CleanCommand,
                    RemoveCommand,
                    LinkCommand,
                    ListCommand,
                    UploadCommand,
                    AddCommand,
                    GetCommand,
                    TouchCommand,
                    NormalizeCommand,
                    ImportCommand,
                    TrashCommand,
                    RemoveEmptyCommand,
                    DecodeCommand
                >, args);
    }
}
