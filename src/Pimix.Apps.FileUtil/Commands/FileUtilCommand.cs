using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using CommandLine;
using NLog;
using NLog.Layouts;
using NLog.Targets;
using Pimix.IO;
using Pimix.Service;

namespace Pimix.Apps.FileUtil.Commands {
    abstract class FileUtilCommand {
        [Option('s', "pimix-server-api-address", HelpText = "Uri for pimix api server address")]
        public string PimixServerAddress { get; set; } =
            ConfigurationManager.AppSettings["PimixServerApiAddress"];

        [Option("pimix-server-credential", HelpText =
            "Credential to access api server, with form: username:password")]
        public string PimixServerCredential { get; set; } =
            ConfigurationManager.AppSettings["PimixServerCredential"];

        [Option("path-map", HelpText =
            "Mapping from path id to actual path on device for local paths")]
        public string PathMap { get; set; } =
            ConfigurationManager.AppSettings["PathMap"];

        [Option("job-id", HelpText = "Job ID to report log as.")]
        public string JobId { get; set; } = null;

        public virtual void Initialize() {
            PimixService.PimixServerApiAddress = PimixServerAddress;
            PimixService.PimixServerCredential =
                Convert.ToBase64String(Encoding.UTF8.GetBytes(PimixServerCredential));

            FileStorageClient.PathMap = PathMap.Split(";")
                .ToDictionary(x => x.Split("=").First(), x => x.Split("=").Last());

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
                        $"{PimixServerAddress}/jobs/$log?id={JobId}&level=${{pad:padding=1:fixedLength=true:inner=${{level}}}}");
                return target;
            }
        }
    }
}
