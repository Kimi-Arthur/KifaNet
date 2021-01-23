using System;
using System.Collections.Generic;
using CommandLine;
using Kifa.Configs;

namespace Kifa.Tools.JobUtil {
    class Program {
        static int Main(string[] args) {
            AppDomain.CurrentDomain.AssemblyLoad +=
                (sender, eventArgs) => KifaConfigs.LoadFromSystemConfigs(eventArgs.LoadedAssembly);

            KifaConfigs.LoadFromSystemConfigs();

            return Parser.Default
                .ParseArguments<RunJobCommand, RunAllJobsCommand, ResetJobCommand>(args)
                .MapResult<JobUtilCommand, int>(ExecuteCommand, HandleParseFail);
        }

        static int ExecuteCommand(JobUtilCommand command) {
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
