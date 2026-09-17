using Kifa.Tools.SubUtil.Commands;

namespace Kifa.Tools.SubUtil;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(args, typeof(GenerateCommand), typeof(UpdateCommand),
            typeof(ImportCommand), typeof(ExtractCommand), typeof(SyncCommand),
            typeof(DownloadSubcatCommand), typeof(MoveCommand));
}
