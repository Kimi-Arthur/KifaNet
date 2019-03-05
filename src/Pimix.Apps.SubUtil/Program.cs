using CommandLine;
using Pimix.Apps.SubUtil.Commands;

namespace Pimix.Apps.SubUtil {
    class Program {
        static int Main(string[] args)
            => PimixCommand.Run(Parser.Default
                .ParseArguments<GenerateCommand, FixCommand, UpdateCommand>(args));
    }
}
