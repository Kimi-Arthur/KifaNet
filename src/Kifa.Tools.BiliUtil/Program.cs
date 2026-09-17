using Kifa.Tools.BiliUtil.Commands;

namespace Kifa.Tools.BiliUtil;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(args, typeof(GetChatCommand), typeof(DownloadVideoCommand),
            typeof(DownloadUploaderCommand), typeof(DownloadBangumiCommand),
            typeof(GetCoverCommand), typeof(LinkCommand), typeof(DownloadMangaCommand),
            typeof(GetTencentChatCommand), typeof(DownloadArchiveCommand), typeof(ShadowCommand),
            typeof(DownloadPlaylistCommand));
}
