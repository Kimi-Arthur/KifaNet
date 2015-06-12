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
using Pimix.IO;

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
                        downloadStream = File.OpenRead($"/allfiles/{uri.Host}{uri.LocalPath}");
                        // Use ftp stream first.
                        //FtpWebRequest request = WebRequest.Create($"ftp://{uri.Host}/files{uri.LocalPath}") as FtpWebRequest;
                        //request.Credentials = new NetworkCredential("pimix", "P2015apr");
                        //downloadStream = request.GetResponse().GetResponseStream();
                    }

                    using (var s = downloadStream)
                    {
                        long len = downloadStream.Length;
                        var info = DataModel.Get<FileInformation>(uri.LocalPath).AddProperties(s, FileProperties.All ^ FileProperties.Path);
                        info.Path = uri.LocalPath;
                        if (info.Locations == null)
                            info.Locations = new Dictionary<string, string>();
                        info.Locations[uri.GetLeftPart(UriPartial.Authority)] = path;
                        if (len == info.Size)
                        {
                            DataModel.Patch(info);
                        }
                    }
                }
            }
        }
    }
}
