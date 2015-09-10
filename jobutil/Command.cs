using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace jobutil
{
    abstract class Command
    {
        [Option('s', "pimix-server-api-address", HelpText = "Uri for pimix api server address.")]
        public string PimixServerAddress { get; set; } = ConfigurationManager.AppSettings["PimixServerApiAddress"];

        [Option('c', "client-name", HelpText = "Client name used to distinguish running jobutil instances.")]
        public string ClientName { get; set; } = ConfigurationManager.AppSettings["ClientName"];

        public abstract int Execute();
    }
}
