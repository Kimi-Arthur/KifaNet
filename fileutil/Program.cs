using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Pimix;
using Pimix.Cloud.BaiduCloud;
using Pimix.IO;
using Pimix.IO.FileFormats;

namespace fileutil
{
    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<InfoCommand, CopyCommand, VerifyCommand, RemoveCommand, MoveCommand>(args)
            .Return(
                (Command x) => ExecuteCommand(x),
                HandleParseFail);
        }

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
            BaiduCloudConfig.PimixServerApiAddress = options.PimixServerAddress;
        }
    }
}
