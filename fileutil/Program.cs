using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pimix;
using Pimix.Cloud.BaiduCloud;
using Pimix.IO;

namespace fileutil
{
    class Program
    {
        static void Main(string[] args)
        {
            int bufferSize = (int)ConfigurationManager.AppSettings["BufferSize"].ParseSizeString();
            BaiduCloudConfig.PimixServerApiAddress = ConfigurationManager.AppSettings["PimixServerApiAddress"];

            switch (args[0])
            {
                case "cp":
                    {
                        Uri input = new Uri(args[1]);
                        //Uri output = new Uri(args[2]);
                        BaiduCloudStorageClient.Config = BaiduCloudConfig.Get("baidu_cloud");
                        var client = new BaiduCloudStorageClient() { AccountId = input.UserInfo };
                        using (var stream = client.GetDownloadStream(input.LocalPath))
                        {
                            using (FileStream fs = new FileStream(args[2], FileMode.Create))
                            {
                                stream.CopyTo(fs, bufferSize);
                            }
                        }

                        break;
                    }
                case "upload":
                    {
                        Uri uploadTo = new Uri(args[2]);
                        BaiduCloudStorageClient.Config = BaiduCloudConfig.Get("baidu_cloud");
                        var client = new BaiduCloudStorageClient() { AccountId = uploadTo.UserInfo };
                        using (var stream = File.OpenRead(args[1]))
                        {
                            client.UploadStream(uploadTo.LocalPath, stream);
                        }

                        break;
                    }
                case "info":
                    {
                        BaiduCloudStorageClient.Config = BaiduCloudConfig.Get("baidu_cloud");
                        string path = args[1];
                        bool dryRun = args.Length > 2 && args[2] == "--dryrun";
                        Uri uri = new Uri(path);
                        Stream downloadStream = null;
                        var schemes = uri.Scheme.Split('+').ToList();
                        if (schemes[0] != "pimix")
                            break;

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
                            var info = FileInformation.Get(uri.LocalPath).AddProperties(s, FileProperties.All ^ FileProperties.Path);
                            info.Path = uri.LocalPath;
                            if (info.Locations == null)
                                info.Locations = new Dictionary<string, string>();
                            info.Locations[uri.GetLeftPart(UriPartial.Authority)] = path;
                            if (len == info.Size)
                            {
                                if (dryRun)
                                {
                                    Console.WriteLine(JsonConvert.SerializeObject(info, Formatting.Indented));
                                }
                                else
                                {
                                    FileInformation.Patch(info);
                                }
                            }
                        }

                        break;
                    }
            }
        }
    }
}
