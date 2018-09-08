using System;
using System.Collections.Generic;
using CommandLine;

namespace Pimix.Apps.SubUtil {
    class Program {
        static int Main(string[] args)
            => Parser.Default
                .ParseArguments<>(args)
                .MapResult<SubUtilCommand, int>(ExecuteCommand, HandleParseFail);

        static int ExecuteCommand(SubUtilCommand command) {
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
