using System;
using System.Collections.Generic;
using CommandLine;
using NLog;
using Pimix.Apps.FileUtil.Commands;
using Pimix.Configs;

namespace Pimix.Apps.FileUtil {
    class Program {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static int Main(string[] args) {
            AppDomain.CurrentDomain.AssemblyLoad +=
                (sender, eventArgs) => PimixConfigs.LoadFromSystemConfigs(eventArgs.LoadedAssembly);

            PimixConfigs.LoadFromSystemConfigs();

            return Parser.Default.ParseArguments<
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
                >(args)
                .MapResult<FileUtilCommand, int>(ExecuteCommand, HandleParseFail);
        }

        static int ExecuteCommand(FileUtilCommand command) {
            command.Initialize();

            try {
                return command.Execute();
            } catch (Exception ex) {
                logger.Fatal(ex, "Unexpected error happened");
                return 1;
            }
        }

        static int HandleParseFail(IEnumerable<Error> errors) => 2;
    }
}
