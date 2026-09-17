using Kifa.Tools.NoteUtil.Commands;

namespace Kifa.Tools.NoteUtil;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(args, typeof(FillCommand), typeof(CollectCommand));
}
