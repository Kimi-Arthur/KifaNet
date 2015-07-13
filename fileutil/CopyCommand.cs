using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Pimix;
using Pimix.Cloud.BaiduCloud;

namespace fileutil
{
    [Verb("cp", HelpText = "Copy file from SOURCE to DEST.")]
    class CopyCommand : Command
    {
        [Value(0, Required = true)]
        public string SourceUri { get; set; }

        [Value(1, Required = true)]
        public string DestinationUri { get; set; }

        [Option('c', "chunk-size", HelpText = "The chunk size used to copy data")]
        public string ChunkSize { get; set; } = ConfigurationManager.AppSettings["BufferSize"];

        public override int Execute()
        {
            Uri uploadTo = new Uri(DestinationUri);
            var schemes = uploadTo.Scheme.Split('+').ToList();

            using (var stream = Helpers.GetDataStream(SourceUri))
            using (var uploadStream = Helpers.GetUploadStream(stream, DestinationUri))
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
                            throw new ArgumentException(nameof(DestinationUri));
                    }
                }
                else
                {
                    using (FileStream fs = new FileStream(Helpers.GetPath(DestinationUri), FileMode.Create))
                    {
                        stream.CopyTo(fs, (int)ChunkSize.ParseSizeString());
                    }
                }
            }

            return 0;
        }
    }
}
