using CommandLine;
using Kifa.Tools.BookUtil.Commands;

namespace Kifa.Tools.BookUtil {
    class Program {
        static int Main(string[] args) =>
            PimixCommand.Run(parameters => Parser.Default.ParseArguments(parameters, typeof(ReorderCommand)), args);
    }
}
