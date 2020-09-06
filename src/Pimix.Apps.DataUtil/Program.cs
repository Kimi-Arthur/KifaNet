using System;
using CommandLine;
using Pimix.Apps.DataUtil.Commands;

namespace Pimix.Apps.DataUtil {
    class Program {
        static int Main(string[] args) =>
            PimixCommand.Run(parameters => Parser.Default.ParseArguments(parameters, typeof(RefreshCommand)), args);
    }
}
