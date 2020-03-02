using CommandLine;
using Pimix.Apps.NoteUtil.Commands;

namespace Pimix.Apps.NoteUtil {
    class Program {
        static int Main(string[] args)
            => PimixCommand.Run(Parser.Default
                .ParseArguments<FillCommand, CleanCommand>, args);
    }
}
