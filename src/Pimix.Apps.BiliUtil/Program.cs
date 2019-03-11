using System;
using System.Collections.Generic;
using CommandLine;
using Pimix.Apps.BiliUtil.Commands;
using Pimix.Configs;


namespace Pimix.Apps.BiliUtil {
    class Program {
        static int Main(string[] args)
            => PimixCommand.Run(Parser.Default
                .ParseArguments<GetChatCommand, RenameVideoCommand, LinkVideoCommand>(args));
    }
}
