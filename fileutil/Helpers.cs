using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Pimix.Cloud.BaiduCloud;
using Pimix.IO;
using Pimix.IO.FileFormats;

namespace fileutil
{
    static class Helpers
    {
        public static Stream GetDataStream(string DataUri)
        {
            Uri uri;
            Stream stream;
            if (Uri.TryCreate(DataUri, UriKind.Absolute, out uri))
            {
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
                                stream = new BaiduCloudStorageClient { AccountId = uri.UserInfo }.GetDownloadStream(uri.LocalPath);
                                break;
                            }
                        default:
                            throw new ArgumentException(nameof(DataUri));
                    }
                }
                else
                {
                    stream = File.OpenRead(GetPath(DataUri));
                    // Use ftp stream first.
                    //FtpWebRequest request = WebRequest.Create($"ftp://{uri.Host}/files{uri.LocalPath}") as FtpWebRequest;
                    //request.Credentials = new NetworkCredential("pimix", "P2015apr");
                    //downloadStream = request.GetResponse().GetResponseStream();
                }

                // Concerning file format
                if (uri.Scheme.Contains("v0"))
                {
                    stream = new PimixFileV0 { Info = FileInformation.Get(uri.LocalPath) }.GetDecodeStream(stream);
                }
                else if (uri.Scheme.Contains("v1"))
                {
                    stream = new PimixFileV1 { Info = FileInformation.Get(uri.LocalPath) }.GetDecodeStream(stream);
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
            if (Uri.TryCreate(uriString, UriKind.Absolute, out uri))
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
                    return new PimixFileV1 { Info = FileInformation.Get(uri.LocalPath) }.GetEncodeStream(stream);
                }
            }

            return stream;
        }
    }
}
