using Kifa.Tools.MemriseUtil.Commands;

namespace Kifa.Tools.MemriseUtil;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(args, typeof(GenerateWordListsCommand),
            typeof(ImportWordListCommand), typeof(ClearWordListCommand));
}
