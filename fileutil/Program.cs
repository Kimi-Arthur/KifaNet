using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pimix;
using Pimix.Cloud.BaiduCloud;
using Pimix.Service;

namespace fileutil
{
    class Program
    {
        static void Main(string[] args)
        {
            int bufferSize = (int)ConfigurationManager.AppSettings["BufferSize"].ParseSizeString();
            DataModel.PimixServerApiAddress = ConfigurationManager.AppSettings["PimixServerApiAddress"];

            switch (args[0])
            {
                case "cp":
                    Uri input = new Uri(args[1]);
                    //Uri output = new Uri(args[2]);
                    BaiduCloudStorageClient.Config = DataModel.Get<BaiduCloudConfig>("baidu_cloud");
                    var client = new BaiduCloudStorageClient() { AccountId = input.UserInfo};
                    using (var stream = client.GetDownloadStream(input.LocalPath))
                    {
                        using (FileStream fs = new FileStream(args[2], FileMode.Create))
                        {
                            stream.CopyTo(fs, bufferSize);
                        }
                    }

                    break;
            }
        }
    }
}
