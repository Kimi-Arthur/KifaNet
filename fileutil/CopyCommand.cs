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

        [Option('p', "precheck", HelpText = "Whether to check (and update) SOURCE before copying.")]
        public bool Precheck { get; set; } = false;

        [Option('u', "update", HelpText = "Whether to update result to server after copying.")]
        public bool Update { get; set; } = false;

        [Option('v', "verify-all", HelpText = "Verify all verifiable fields before update.")]
        public bool VerifyAll { get; set; } = false;

        [Option('f', "fields-to-verify", HelpText = "Fields to verify. Only 'Size' is verified by default.")]
        public string FieldsToVerify { get; set; } = "Size";

        public override int Execute()
        {
            if (Precheck)
            {
                var result = new InfoCommand { Update = true, VerifyAll = true, FileUri = SourceUri }.Execute();
                if (result != 0)
                {
                    Console.Error.WriteLine("Precheck failed!");
                    return 1;
                }
            }

            using (var stream = Helpers.GetDataStream(SourceUri))
            using (var uploadStream = Helpers.GetUploadStream(stream, DestinationUri))
            {
                Uri uploadTo;
                if (Uri.TryCreate(DestinationUri, UriKind.Absolute, out uploadTo))
                {
                    var schemes = uploadTo.Scheme.Split('+').ToList();
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
                }
                else
                {
                    using (FileStream fs = new FileStream(Helpers.GetPath(DestinationUri), FileMode.Create))
                    {
                        stream.CopyTo(fs, (int)ChunkSize.ParseSizeString());
                    }
                }
            }

            return Update ? new InfoCommand { Update = true, VerifyAll = VerifyAll, FieldsToVerify = FieldsToVerify, FileUri = DestinationUri }.Execute() : 0;
        }
    }
}
