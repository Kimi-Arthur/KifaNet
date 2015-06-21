using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
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
            var commands = new Dictionary<Type, Action<object>>()
            {
                [typeof(InfoCommandOptions)] = (x) => GetInfo(x as InfoCommandOptions),
                [typeof(CopyCommandOptions)] = (x) => CopyFile(x as CopyCommandOptions),
                [typeof(UploadCommandOptions)] = (x) => UploadFile(x as UploadCommandOptions)
            };

            var result = Parser.Default.ParseArguments<CopyCommandOptions, InfoCommandOptions, UploadCommandOptions>(args);
            if (!result.Errors.Any())
            {
                Initialize();

                commands[result.Value.GetType()](result.Value);
            }
        }

        static void Initialize()
        {
            BaiduCloudConfig.PimixServerApiAddress = ConfigurationManager.AppSettings["PimixServerApiAddress"];
        }

        static void CopyFile(CopyCommandOptions options)
        {
            Uri input = new Uri(options.SourceUri);
            BaiduCloudStorageClient.Config = BaiduCloudConfig.Get("baidu_cloud");
            var client = new BaiduCloudStorageClient() { AccountId = input.UserInfo };
            using (var stream = client.GetDownloadStream(input.LocalPath))
            {
                using (FileStream fs = new FileStream(options.DestinationUri, FileMode.Create))
                {
                    stream.CopyTo(fs, (int)options.ChunkSize.ParseSizeString());
                }
            }
        }

        static void UploadFile(UploadCommandOptions options)
        {
            Uri uploadTo = new Uri(options.DestinationUri);
            BaiduCloudStorageClient.Config = BaiduCloudConfig.Get("baidu_cloud");
            var client = new BaiduCloudStorageClient() { AccountId = uploadTo.UserInfo };
            using (var stream = File.OpenRead(options.SourceUri))
            {
                client.UploadStream(uploadTo.LocalPath, stream);
            }
        }

        static void GetInfo(InfoCommandOptions options)
        {
            BaiduCloudStorageClient.Config = BaiduCloudConfig.Get("baidu_cloud");
            Uri uri = new Uri(options.FileUri);
            Stream downloadStream = null;
            var schemes = uri.Scheme.Split('+').ToList();
            if (schemes[0] != "pimix")
                return;

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
                info.Locations[uri.GetLeftPart(UriPartial.Authority)] = options.FileUri;
                if (len == info.Size)
                {
                    if (options.Dryrun)
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(info, Formatting.Indented));
                    }
                    else
                    {
                        FileInformation.Patch(info);
                    }
                }
            }

        }
    }
}
