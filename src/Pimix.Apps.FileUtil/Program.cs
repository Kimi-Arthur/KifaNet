using CommandLine;
using Pimix.Apps.FileUtil.Commands;

namespace Pimix.Apps.FileUtil {
    class Program {
        static int Main(string[] args) =>
            PimixCommand.Run(
                parameters => Parser.Default.ParseArguments(parameters,
                    new[] {
                        typeof(CheckCommand), typeof(CleanCommand), typeof(RemoveCommand), typeof(LinkCommand),
                        typeof(ListCommand), typeof(UploadCommand), typeof(AddCommand), typeof(GetCommand),
                        typeof(TouchCommand), typeof(NormalizeCommand), typeof(ImportCommand), typeof(TrashCommand),
                        typeof(RemoveEmptyCommand), typeof(DecodeCommand), typeof(DedupCommand)
                    }), args);
    }
}
