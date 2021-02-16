using CommandLine;
using Kifa.Tools.NoteUtil.Commands;

namespace Kifa.Tools.NoteUtil {
    class Program {
        static int Main(string[] args)
            => KifaCommand.Run(Parser.Default
                .ParseArguments<FillCommand, CollectCommand>, args);
    }
}
