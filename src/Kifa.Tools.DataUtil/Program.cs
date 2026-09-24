using Kifa.Tools.DataUtil.Commands;

namespace Kifa.Tools.DataUtil;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(args, typeof(ImportCommand), typeof(ExportCommand),
            typeof(LinkCommand), typeof(AddCommand), typeof(SyncCommand),
            typeof(DeleteCommand), typeof(CallCommand));
}
