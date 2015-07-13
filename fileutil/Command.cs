using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace fileutil
{
    class Command
    {
        [Option('s', "pimix-server-api-address", HelpText = "Uri for pimix api server address")]
        public string PimixServerAddress { get; set; } = ConfigurationManager.AppSettings["PimixServerApiAddress"];

        public virtual int Execute()
        {
            throw new NotImplementedException();
        }
    }
}
