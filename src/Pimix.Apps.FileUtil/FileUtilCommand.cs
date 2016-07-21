using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using CommandLine;
using Pimix.Cloud.BaiduCloud;
using Pimix.IO;
using Pimix.IO.FileFormats;

namespace Pimix.Apps.FileUtil
{
    abstract class FileUtilCommand
    {
        [Option('s', "pimix-server-api-address", HelpText = "Uri for pimix api server address")]
        public string PimixServerAddress { get; set; } = ConfigurationManager.AppSettings["PimixServerApiAddress"];

        [Option('g', "storage-server-order", HelpText = "Storage server order separated by semicolons.")]
        public string StorageServerOrder { get; set; } = ConfigurationManager.AppSettings["StorageServerOrder"];

        public IEnumerable<string> StorageServerOrderList
            => StorageServerOrder.Split(';');

        public Stream GetDataStream(string DataUri)
        {
            Uri uri;
            Stream stream;
            if (Uri.TryCreate(DataUri, UriKind.Absolute, out uri))
            {
                if (string.IsNullOrEmpty(uri.Host))
                {
                    var info = FileInformation.Get(uri.LocalPath);
                    foreach (var g in StorageServerOrderList)
                    {
                        if (info.Locations.ContainsKey(g))
                        {
                            Console.Error.WriteLine($"{info.Locations[g]} chosen as the source uri.");
                            uri = new Uri(info.Locations[g]);
                            break;
                        }
                    }
                }

                var schemes = uri.Scheme.Split('+').ToList();
                if (schemes[0] == "http" || schemes[0] == "https")
                {
                    HttpWebRequest request = WebRequest.CreateHttp(uri);
                    request.Method = "GET";

                    var response = request.GetResponse();
                    return response.GetResponseStream();
                }

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
                                stream = new BaiduCloudStorageClient { AccountId = uri.UserInfo }.OpenRead(uri.LocalPath);
                                break;
                            }
                        default:
                            throw new ArgumentException(nameof(DataUri));
                    }
                }
                else
                {
                    stream = File.OpenRead(GetPath(DataUri));
                }

                // Concerning file format
                if (uri.Scheme.Contains("v0"))
                {
                    stream = new PimixFileV0Format { Info = FileInformation.Get(uri.LocalPath) }.GetDecodeStream(stream);
                }
                else if (uri.Scheme.Contains("v1"))
                {
                    stream = new PimixFileV1Format { Info = FileInformation.Get(uri.LocalPath) }.GetDecodeStream(stream);
                }

                return stream;
            }
            else
            {
                // Only paths supported by system is allowed.
                return File.OpenRead(DataUri);
            }
        }

        public static string GetPath(string uriString)
        {
            Uri uri;
            if (Uri.TryCreate(uriString, UriKind.Absolute, out uri) && uri.Scheme.StartsWith("pimix"))
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    return $"/allfiles/{uri.Host}{uri.LocalPath}";
                }
                else
                {
                    return $"\\\\{uri.Host.Split('.')[0]}/files{uri.LocalPath}";
                }
            }
            else
            {
                return uriString;
            }
        }

        public static Stream GetUploadStream(Stream stream, string uploadUri)
        {
            Uri uri;
            if (Uri.TryCreate(uploadUri, UriKind.Absolute, out uri))
            {
                var schemes = uri.Scheme.Split('+').ToList();
                if (schemes[0] != "pimix")
                    throw new ArgumentException(nameof(uploadUri));

                if (schemes.Contains("v1"))
                {
                    return new PimixFileV1Format { Info = FileInformation.Get(uri.LocalPath) }.GetEncodeStream(stream);
                }
            }

            return stream;
        }

        public static StorageClient GetStorageClient(String location)
        {
            Uri uri;
            if (Uri.TryCreate(location, UriKind.Absolute, out uri))
            {
                var schemes = uri.Scheme.Split('+').ToList();

                if (schemes.Contains("cloud"))
                {
                    switch (uri.Host)
                    {
                        case "pan.baidu.com":
                            return new BaiduCloudStorageClient { AccountId = uri.UserInfo };
                        default:
                            throw new ArgumentException(nameof(location));
                    }
                }
            }

            return new FileStorageClient();
        }

        public virtual void Initialize()
        {
            BaiduCloudConfig.PimixServerApiAddress = PimixServerAddress;
        }

        public abstract int Execute();
    }
}
