using System;
using System.Collections.Generic;
using CommandLine;

namespace Pimix.Apps.JobUtil
{
    class Program
    {
        static int Main(string[] args)
            => Parser.Default.ParseArguments<RunJobCommand, RunAllJobsCommand, ResetJobCommand>(args)
            .MapResult<JobUtilCommand, int>(ExecuteCommand, HandleParseFail);

        static int ExecuteCommand(JobUtilCommand command)
        {
            command.Initialize();

            try
            {
                return command.Execute();
            }
            catch (Exception ex)
            {
                while (ex != null)
                {
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
