using System.Configuration;
using System.Net;
using CommandLine;
using NLog;
using NLog.Layouts;
using NLog.Targets;
using Pimix.Service;

namespace Pimix.Apps.FileUtil.Commands {
    abstract class FileUtilCommand {
        [Option("job-id", HelpText = "Job ID to report log as.")]
        public string JobId { get; set; } = null;

        public void Initialize() {
            var config = LogManager.Configuration;
            if (JobId != null) {
                config.RemoveTarget("console");
                config.AddTarget(ServiceTarget);
            }

            CredentialCache.DefaultNetworkCredentials.Domain =
                ConfigurationManager.AppSettings["DefaultNetworkDomain"];
            CredentialCache.DefaultNetworkCredentials.UserName =
                ConfigurationManager.AppSettings["DefaultNetworkUserName"];
            CredentialCache.DefaultNetworkCredentials.Password =
                ConfigurationManager.AppSettings["DefaultNetworkPassword"];
        }

        public abstract int Execute();

        Target ServiceTarget {
            get {
                var target = new NetworkTarget();
                target.Address =
                    Layout.FromString(
                        $"{PimixService.PimixServerApiAddress}/jobs/$log?id={JobId}&level=${{pad:padding=1:fixedLength=true:inner=${{level}}}}");
                return target;
            }
        }
    }
}
