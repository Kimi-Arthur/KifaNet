using System;
using System.Collections.Generic;
using CommandLine;
using Pimix.Cloud.BaiduCloud;

namespace fileutil
{
    class Program
    {
        static int Main(string[] args)
            => Parser.Default.ParseArguments<
                InfoCommand,
                CopyCommand,
                VerifyCommand,
                RemoveCommand,
                MoveCommand,
                LinkCommand
                >(args)
            .Return<FileUtilCommand, int>(ExecuteCommand, HandleParseFail);

        static int ExecuteCommand(FileUtilCommand command)
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
