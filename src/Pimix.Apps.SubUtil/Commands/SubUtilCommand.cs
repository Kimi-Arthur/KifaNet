using System;
using System.Configuration;
using System.Text;
using CommandLine;
using Pimix.Service;

namespace Pimix.Apps.SubUtil.Commands {
    abstract class SubUtilCommand {
        [Option('s', "pimix-server-api-address", HelpText = "Uri for pimix api server address")]
        public string PimixServerAddress { get; set; } =
            ConfigurationManager.AppSettings["PimixServerApiAddress"];

        [Option("pimix-server-credential", HelpText =
            "Credential to access api server, with form: username:password")]
        public string PimixServerCredential { get; set; } =
            ConfigurationManager.AppSettings["PimixServerCredential"];

        public void Initialize() {
            PimixService.PimixServerApiAddress = PimixServerAddress;
            PimixService.PimixServerCredential =
                Convert.ToBase64String(Encoding.UTF8.GetBytes(PimixServerCredential));
        }

        public abstract int Execute();
    }
}
