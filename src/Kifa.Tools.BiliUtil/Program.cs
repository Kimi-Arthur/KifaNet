using System;
using CommandLine;
using Kifa.Tools.BiliUtil.Commands;

namespace Kifa.Tools.BiliUtil;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(new Parser(settings => {
            settings.CaseInsensitiveEnumValues = true;
            settings.HelpWriter = Console.Error;
        }).ParseArguments<GetChatCommand, DownloadVideoCommand, DownloadUploaderCommand,
            DownloadBangumiCommand, GetCoverCommand, LinkCommand, DownloadMangaCommand,
            GetTencentChatCommand, DownloadArchiveCommand, ShadowCommand>, args);
}
