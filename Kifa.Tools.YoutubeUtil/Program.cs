using Kifa.Tools.YoutubeUtil.Commands;

namespace Kifa.Tools.YoutubeUtil;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(args, typeof(DownloadVideoCommand),
            typeof(DownloadPlaylistCommand), typeof(DownloadUploaderCommand),
            typeof(AddUploaderNameCommand), typeof(ImportVideoCommand));
}
