using System;
using System.Collections.Generic;
using CommandLine;
using Pimix.Apps.BiliUtil.Commands;
using Pimix.Configs;


namespace Pimix.Apps.BiliUtil {
    class Program {
        static int Main(string[] args) {
            return Parser.Default
                .ParseArguments<GetChatCommand, RenameVideoCommand>(args)
                .MapResult<PimixCommand, int>(ExecuteCommand, HandleParseFail);
        }

        static int ExecuteCommand(PimixCommand command) {
            command.Initialize();
            try {
                return command.Execute();
            } catch (Exception ex) {
                while (ex != null) {
                    Console.WriteLine("Caused by:");
                    Console.WriteLine(ex);
                    ex = ex.InnerException;
                }

                return 1;
            }
        }

        static int HandleParseFail(IEnumerable<Error> errors) => 2;
    }
}
