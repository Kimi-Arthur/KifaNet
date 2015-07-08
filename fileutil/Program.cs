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
            var result = Parser.Default.ParseArguments<InfoCommand, CopyCommand, VerifyCommand>(args);
            if (!result.Errors.Any())
            {
                Initialize(result.Value as Command);

                try
                {
                    return (result.Value as Command).Execute();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    while (ex.InnerException != null)
                    {
                        Console.WriteLine("Caused by:");
                        ex = ex.InnerException;
                        Console.WriteLine(ex);
                    }

                    return 1;
                }
            }

            return 2;
        }

        static void Initialize(Command options)
        {
            BaiduCloudConfig.PimixServerApiAddress = options.PimixServerAddress;
        }
    }
}
