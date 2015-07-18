using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace jobutil
{
    class Program
    {
        static int Main(string[] args)
            => Parser.Default.ParseArguments<RunJobCommand>(args)
            .Return(
                x => ExecuteCommand(x),
                HandleParseFail);

        static int ExecuteCommand(Command command)
        {
            Initialize(command);

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

        static void Initialize(Command options)
        {
            Job.PimixServerApiAddress = options.PimixServerAddress;
        }
    }
}
