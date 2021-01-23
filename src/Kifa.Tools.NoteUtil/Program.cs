using CommandLine;
using Kifa.Tools.NoteUtil.Commands;

namespace Kifa.Tools.NoteUtil {
    class Program {
        static int Main(string[] args)
            => PimixCommand.Run(Parser.Default
                .ParseArguments<FillCommand, CollectCommand>, args);
    }
}
