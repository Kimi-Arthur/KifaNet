using CommandLine;
using Kifa.Tools.MemriseUtil.Commands;

namespace Kifa.Tools.MemriseUtil;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(Parser.Default.ParseArguments<UploadAudioCommand, ImportWordListCommand>,
            args);
}
