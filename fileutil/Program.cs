using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pimix;
using Pimix.Cloud.BaiduCloud;

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
            }
        }
    }
}
