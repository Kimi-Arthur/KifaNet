using Kifa.Tools.BookUtil.Commands;

namespace Kifa.Tools.BookUtil;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(args, typeof(ReorderCommand), typeof(CreatePdfMangaCommand),
            typeof(CreateMangaCommand));
}
