using System.Collections.Generic;
using System.Configuration;
using System.Net;
using CommandLine;
using Pimix.Cloud.BaiduCloud;

namespace Pimix.Apps.FileUtil.Commands {
    abstract class FileUtilCommand {
        [Option('s', "pimix-server-api-address", HelpText = "Uri for pimix api server address")]
        public string PimixServerAddress { get; set; } = ConfigurationManager.AppSettings["PimixServerApiAddress"];

        [Option('g', "storage-server-order", HelpText = "Storage server order separated by semicolons.")]
        public string StorageServerOrder { get; set; } = ConfigurationManager.AppSettings["StorageServerOrder"];

        public IEnumerable<string> StorageServerOrderList
            => StorageServerOrder.Split(';');

        public virtual void Initialize() {
            BaiduCloudConfig.PimixServerApiAddress = PimixServerAddress;

            CredentialCache.DefaultNetworkCredentials.Domain = ConfigurationManager.AppSettings["DefaultNetworkDomain"];
            CredentialCache.DefaultNetworkCredentials.UserName = ConfigurationManager.AppSettings["DefaultNetworkUserName"];
            CredentialCache.DefaultNetworkCredentials.Password = ConfigurationManager.AppSettings["DefaultNetworkPassword"];
        }

        public abstract int Execute();
    }
}
