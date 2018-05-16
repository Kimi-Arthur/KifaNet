using System;
using System.Configuration;
using System.Text;
using CommandLine;
using Pimix.Service;

namespace Pimix.Apps.JobUtil {
    abstract class JobUtilCommand {
        [Option('s', "pimix-server-api-address", HelpText = "Uri for pimix api server address.")]
        public string PimixServerAddress { get; set; } =
            ConfigurationManager.AppSettings["PimixServerApiAddress"];

        [Option("pimix-server-credential", HelpText =
            "Credential to access api server, with form: username:password")]
        public string PimixServerCredential { get; set; } =
            ConfigurationManager.AppSettings["PimixServerCredential"];

        [Option('c', "client-name", HelpText =
            "Client name used to distinguish running jobutil instances.")]
        public string ClientName { get; set; } = ConfigurationManager.AppSettings["ClientName"];

        public TimeSpan HeartbeatInterval { get; set; } =
            ConfigurationManager.AppSettings["HeartbeatInterval"].ParseTimeSpanString();

        [Option('b', "fire-heartbeat", HelpText =
            "Whether to fire heartbeat during job execution.")]
        public bool FireHeartbeat { get; set; } = false;

        public virtual void Initialize() {
            PimixService.PimixServerApiAddress = PimixServerAddress;
            PimixService.PimixServerCredential =
                Convert.ToBase64String(Encoding.UTF8.GetBytes(PimixServerCredential));
        }

        public abstract int Execute();
    }
}
