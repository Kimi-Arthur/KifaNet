using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using CommandLine;
using NLog;
using NLog.Layouts;
using NLog.Targets;
using Pimix.Cloud.BaiduCloud;

namespace Pimix.Apps.FileUtil.Commands {
    abstract class FileUtilCommand {
        [Option('s', "pimix-server-api-address", HelpText = "Uri for pimix api server address")]
        public string PimixServerAddress { get; set; } = ConfigurationManager.AppSettings["PimixServerApiAddress"];

        [Option("job-id", HelpText = "Job ID to report log as.")]
        public string JobId { get; set; } = null;

        public virtual void Initialize() {
            BaiduCloudConfig.PimixServerApiAddress = PimixServerAddress;
            var config = LogManager.Configuration;
            if (JobId != null) {
                config.RemoveTarget("console");
                config.AddTarget(ServiceTarget);
            }

            CredentialCache.DefaultNetworkCredentials.Domain = ConfigurationManager.AppSettings["DefaultNetworkDomain"];
            CredentialCache.DefaultNetworkCredentials.UserName = ConfigurationManager.AppSettings["DefaultNetworkUserName"];
            CredentialCache.DefaultNetworkCredentials.Password = ConfigurationManager.AppSettings["DefaultNetworkPassword"];
        }

        public abstract int Execute();

        Target ServiceTarget {
            get {
                var target = new NetworkTarget();
                target.Address = Layout.FromString($"{PimixServerAddress}/jobs/$log?id={JobId}&level=${{pad:padding=1:fixedLength=true:inner=${{level}}}}");
                return target;
            }
        }
    }

}
