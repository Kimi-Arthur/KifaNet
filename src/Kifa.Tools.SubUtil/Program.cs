using CommandLine;
using Kifa.Tools.SubUtil.Commands;

namespace Kifa.Tools.SubUtil; 

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(Parser.Default
            .ParseArguments<GenerateCommand, FixCommand, UpdateCommand, CleanCommand, ImportCommand>, args);
}