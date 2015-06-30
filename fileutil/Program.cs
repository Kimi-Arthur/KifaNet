using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
                [typeof(UploadCommandOptions)] = x => UploadFile(x as UploadCommandOptions),
                [typeof(VerifyCommand)] = x => (x as VerifyCommand).Execute()
            };

            var result = Parser.Default.ParseArguments<InfoCommandOptions, UploadCommandOptions, VerifyCommand>(args);
            if (!result.Errors.Any())
            {
                Initialize(result.Value as Command);

                commands[result.Value.GetType()](result.Value);
            }
        }

        static void Initialize(Command options)
        {
            BaiduCloudConfig.PimixServerApiAddress = options.PimixServerAddress;
        }

        static void UploadFile(UploadCommandOptions options)
        {
            Uri uploadTo = new Uri(options.DestinationUri);
            var schemes = uploadTo.Scheme.Split('+').ToList();

            using (var stream = Helpers.GetDataStream(options.SourceUri))
            using (var uploadStream = GetUploadStream(stream, options.DestinationUri))
            {
                if (schemes.Contains("cloud"))
                {
                    switch (uploadTo.Host)
                    {
                        case "pan.baidu.com":
                            {
                                BaiduCloudStorageClient.Config = BaiduCloudConfig.Get("baidu_cloud");
                                new BaiduCloudStorageClient { AccountId = uploadTo.UserInfo }.UploadStream(uploadTo.LocalPath, uploadStream, tryRapid: false);
                                break;
                            }
                        default:
                            throw new ArgumentException(nameof(options));
                    }
                }
                else
                {
                    using (FileStream fs = new FileStream(Helpers.GetPath(options.DestinationUri), FileMode.Create))
                    {
                        stream.CopyTo(fs, (int)options.ChunkSize.ParseSizeString());
                    }
                }
            }
        }

        static void GetInfo(InfoCommandOptions options)
        {
            Uri uri = new Uri(options.FileUri);

            using (var stream = Helpers.GetDataStream(options.FileUri))
            {
                long len = stream.Length;
                var info = FileInformation.Get(uri.LocalPath).AddProperties(stream, FileProperties.All);
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

        static Stream GetUploadStream(Stream stream, string uploadUri)
        {
            Uri uri;
            if (Uri.TryCreate(uploadUri, UriKind.Absolute, out uri))
            {
                var schemes = uri.Scheme.Split('+').ToList();
                if (schemes[0] != "pimix")
                    throw new ArgumentException(nameof(uploadUri));

                if (schemes.Contains("v1"))
                {
                    return new PimixFileV1 { Info = FileInformation.Get(uri.LocalPath) }.GetEncodeStream(stream);
                }
            }

            return stream;
        }
    }
}
