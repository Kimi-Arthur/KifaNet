using System;
using CommandLine;
using Kifa.Tools.DataUtil.Commands;

namespace Kifa.Tools.DataUtil {
    class Program {
        static int Main(string[] args) =>
            PimixCommand.Run(parameters => Parser.Default.ParseArguments(parameters, typeof(RefreshCommand)), args);
    }
}
