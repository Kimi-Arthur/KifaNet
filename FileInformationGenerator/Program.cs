using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pimix.Cloud.Baidu;
using Pimix.Service;
using Pimix.Storage;

namespace FileInformationGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string site = ConfigurationManager.AppSettings["Site"];
            string pathPrefix = ConfigurationManager.AppSettings["PathPrefix"];

            DataModel.PimixServerApiAddress = ConfigurationManager.AppSettings["PimixServerApiAddress"];
            DataModel.PimixServerCredential = ConfigurationManager.AppSettings["PimixServerCredential"];

            foreach (var arg in args)
            {
                foreach (var path in arg.Split('\n'))
                {
                    Uri uri = new Uri(path);
                    //var info = FileUtility.GetInformation($"{pathPrefix}/{path}", FileProperties.All ^ FileProperties.Path);
                    var client = new StorageClient() { AccountId = uri.UserInfo };
                    using (var s = client.GetDownloadStream(uri.AbsolutePath))
                    {
                        var info = FileUtility.GetInformation(s, FileProperties.All ^ FileProperties.Path);
                        info.Id = path;
                        info.Path = path;
                        info.Locations = new List<string> { $"{site}:{path}" };
                        DataModel.Patch(info);
                    }
                }
            }
        }
    }
}
