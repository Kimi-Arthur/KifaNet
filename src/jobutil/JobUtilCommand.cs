using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Pimix;

namespace jobutil
{
    abstract class JobUtilCommand
    {
        [Option('s', "pimix-server-api-address", HelpText = "Uri for pimix api server address.")]
        public string PimixServerAddress { get; set; } = ConfigurationManager.AppSettings["PimixServerApiAddress"];

        [Option('c', "client-name", HelpText = "Client name used to distinguish running jobutil instances.")]
        public string ClientName { get; set; } = ConfigurationManager.AppSettings["ClientName"];

        public TimeSpan HeartbeatInterval { get; set; } = ConfigurationManager.AppSettings["HeartbeatInterval"].ParseTimeSpanString();

        [Option('b', "fire-heartbeat", HelpText = "Whether to fire heartbeat during job execution.")]
        public bool FireHeartbeat { get; set; } = false;

        public virtual void Initialize()
        {
            Job.PimixServerApiAddress = PimixServerAddress;
        }

        public abstract int Execute();
    }
}
