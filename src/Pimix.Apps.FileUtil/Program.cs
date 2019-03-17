using System;
using System.Collections.Generic;
using CommandLine;
using NLog;
using Pimix.Apps.FileUtil.Commands;
using Pimix.Configs;

namespace Pimix.Apps.FileUtil {
    class Program {
        static int Main(string[] args)
            => PimixCommand.Run(() => Parser.Default
                .ParseArguments<
                    InfoCommand,
                    CopyCommand,
                    VerifyCommand,
                    RemoveCommand,
                    MoveCommand,
                    LinkCommand,
                    ListCommand,
                    UploadCommand,
                    AddCommand,
                    GetCommand,
                    TouchCommand
                >(args));
    }
}
