using System;
using CommandLine;
using Kifa.Tools.YoutubeUtil.Commands;

namespace Kifa.Tools.YoutubeUtil;

class Program {
    static int Main(string[] args)
        => KifaCommand.Run(
            parameters
                => new Parser(settings => {
                    settings.CaseInsensitiveEnumValues = true;
                    settings.HelpWriter = Console.Error;
                    settings.EnableDashDash = true;
                }).ParseArguments(parameters, typeof(DownloadVideoCommand),
                    typeof(DownloadPlaylistCommand), typeof(DownloadUploaderCommand)), args);
}
