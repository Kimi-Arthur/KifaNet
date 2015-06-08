using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pimix.Cloud.BaiduCloud;
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
            BaiduCloudStorageClient.Config = DataModel.Get<BaiduCloudConfig>("baidu_cloud");

            foreach (var arg in args)
            {
                foreach (var path in arg.Split('\n'))
                {
                    Uri uri = new Uri(path);
                    Stream downloadStream = null;
                    var schemes = uri.Scheme.Split('+').ToList();
                    if (schemes[0] != "pimix")
                        continue;

                    if (schemes.Contains("cloud"))
                    {
                        switch (uri.Host)
                        {
                            case "pan.baidu.com":
                                {
                                    var client = new BaiduCloudStorageClient
                                    {
                                        AccountId = uri.UserInfo
                                    };
                                    downloadStream = client.GetDownloadStream(uri.LocalPath);
                                    break;
                                }
                            default:
                                break;
                        }
                    }
                    else
                    {
                        downloadStream = File.OpenRead($"\\\\{uri.UserInfo}@{uri.Host}/files{uri.LocalPath}");
                        // Use ftp stream first.
                        //FtpWebRequest request = WebRequest.Create($"ftp://{uri.Host}/files{uri.LocalPath}") as FtpWebRequest;
                        //request.Credentials = new NetworkCredential("pimix", "P2015apr");
                        //downloadStream = request.GetResponse().GetResponseStream();
                    }

                    using (var s = downloadStream)
                    {
                        var info = FileInformation.GetInformation(s, FileProperties.All ^ FileProperties.Path);
                        info.Id = uri.LocalPath;
                        info.Path = uri.LocalPath;
                        info.Locations = new List<string> { path };
                        DataModel.Patch(info);
                    }
                }
            }
        }
    }
}
