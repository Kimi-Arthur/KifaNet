using CommandLine;
using Kifa.Tools.DataUtil.Commands;

namespace Kifa.Tools.DataUtil;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(
            parameters => Parser.Default.ParseArguments(parameters, typeof(ImportCommand),
                typeof(ExportCommand), typeof(LinkCommand), typeof(AddCommand), typeof(SyncCommand)), args);
}
