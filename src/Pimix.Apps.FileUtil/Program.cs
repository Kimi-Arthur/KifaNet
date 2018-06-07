using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using NLog;
using Pimix.Apps.FileUtil.Commands;

namespace Pimix.Apps.FileUtil {
    class Program {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static int Main(string[] args) {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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
                    GetCommand
                >(args)
                .MapResult<FileUtilCommand, int>(ExecuteCommand, HandleParseFail);
        }

        static int ExecuteCommand(FileUtilCommand command) {
            command.Initialize();

            try {
                return command.Execute();
            } catch (Exception ex) {
                logger.Fatal(ex, "fileutil terminated.");
                return 1;
            }
        }

        static int HandleParseFail(IEnumerable<Error> errors) => 2;
    }
}
