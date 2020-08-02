using CommandLine;
using Pimix.Apps.BookUtil.Commands;

namespace Pimix.Apps.BookUtil {
    class Program {
        static int Main(string[] args) =>
            PimixCommand.Run(parameters => Parser.Default.ParseArguments(parameters, typeof(ReorderCommand)), args);
    }
}
