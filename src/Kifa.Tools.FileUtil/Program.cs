using CommandLine;
using Kifa.Tools.FileUtil.Commands;

namespace Kifa.Tools.FileUtil {
    class Program {
        static int Main(string[] args) =>
            KifaCommand.Run(
                parameters => Parser.Default.ParseArguments(parameters,
                    new[] {
                        typeof(CheckCommand), typeof(CleanCommand), typeof(RemoveCommand), typeof(CopyCommand),
                        typeof(ListCommand), typeof(UploadCommand), typeof(AddCommand), typeof(GetCommand),
                        typeof(TouchCommand), typeof(NormalizeCommand), typeof(ImportCommand), typeof(TrashCommand),
                        typeof(RemoveEmptyCommand), typeof(DecodeCommand), typeof(DedupCommand)
                    }), args);
    }
}
