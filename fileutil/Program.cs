using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Pimix;
using Pimix.Cloud.BaiduCloud;
using Pimix.IO;
using Pimix.IO.FileFormats;

namespace fileutil
{
    class Program
    {
        static void Main(string[] args)
        {
            var commands = new Dictionary<Type, Action<object>>()
            {
                [typeof(InfoCommandOptions)] = x => GetInfo(x as InfoCommandOptions),
                [typeof(CopyCommandOptions)] = x => CopyFile(x as CopyCommandOptions),
                [typeof(UploadCommandOptions)] = x => UploadFile(x as UploadCommandOptions)
            };

            var result = Parser.Default.ParseArguments<CopyCommandOptions, InfoCommandOptions, UploadCommandOptions>(args);
            if (!result.Errors.Any())
            {
                Initialize(result.Value as CommandLineOptions);

                commands[result.Value.GetType()](result.Value);
            }
        }

        static Stream GetStream(string DataUri)
        {
            Uri uri;
            Stream stream;
            if (Uri.TryCreate(DataUri, UriKind.Absolute, out uri))
            {
                var schemes = uri.Scheme.Split('+').ToList();
                if (schemes[0] != "pimix")
                    throw new ArgumentException(nameof(DataUri));

                // Concerning file source
                if (schemes.Contains("cloud"))
                {
                    switch (uri.Host)
                    {
                        case "pan.baidu.com":
                            {
                                BaiduCloudStorageClient.Config = BaiduCloudConfig.Get("baidu_cloud");
                                stream = new BaiduCloudStorageClient { AccountId = uri.UserInfo }.GetDownloadStream(uri.LocalPath);
                                break;
                            }
                        default:
                            throw new ArgumentException(nameof(DataUri));
                    }
                }
                else
                {
                    stream = File.OpenRead($"/allfiles/{uri.Host}{uri.LocalPath}");
                    // Use ftp stream first.
                    //FtpWebRequest request = WebRequest.Create($"ftp://{uri.Host}/files{uri.LocalPath}") as FtpWebRequest;
                    //request.Credentials = new NetworkCredential("pimix", "P2015apr");
                    //downloadStream = request.GetResponse().GetResponseStream();
                }

                // Concerning file format
                if (uri.Scheme.Contains("v0"))
                {
                    stream = new PimixFileV0() { Info = FileInformation.Get(uri.LocalPath) }.GetDecodeStream(stream);
                }
                else if (uri.Scheme.Contains("v1"))
                {
                    stream = new PimixFileV1() { Info = FileInformation.Get(uri.LocalPath) }.GetDecodeStream(stream);
                }

                return stream;
            }
            else
            {
                // Only paths supported by system is allowed.
                return File.OpenRead(DataUri);
            }
        }

        static void Initialize(CommandLineOptions options)
        {
            BaiduCloudConfig.PimixServerApiAddress = options.PimixServerAddress;
        }

        static void CopyFile(CopyCommandOptions options)
        {
            Uri input = new Uri(options.SourceUri);
            using (var stream = GetStream(options.SourceUri))
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
            using (var stream = GetStream(options.SourceUri))
            {
                client.UploadStream(uploadTo.LocalPath, stream);
            }
        }

        static void GetInfo(InfoCommandOptions options)
        {
            Uri uri = new Uri(options.FileUri);

            using (var stream = GetStream(options.FileUri))
            {
                long len = stream.Length;
                var info = FileInformation.Get(uri.LocalPath).AddProperties(stream, FileProperties.All ^ FileProperties.Path);
                info.Path = uri.LocalPath;
                if (info.Locations == null)
                    info.Locations = new Dictionary<string, string>();
                info.Locations[$"{uri.Scheme}://{uri.Host}"] = options.FileUri;
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
