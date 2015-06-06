using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pimix.Cloud.BaiduCloud;
using Pimix.Service;

namespace fileutil
{
    class Program
    {
        static void Main(string[] args)
        {
            switch (args[0])
            {
                case "cp":
                    Uri input = new Uri(args[1]);
                    //Uri output = new Uri(args[2]);
                    DataModel.PimixServerApiAddress = "http://test.pimix.org/api";
                    BaiduCloudStorageClient.Config = DataModel.Get<BaiduCloudConfig>("baidu_cloud");
                    var client = new BaiduCloudStorageClient() { AccountId = input.UserInfo};
                    using (var stream = client.GetDownloadStream(input.AbsolutePath))
                    {
                        using (FileStream fs = new FileStream(args[2], FileMode.Create))
                        {
                            stream.CopyTo(fs);
                        }
                    }

                    break;
            }
        }
    }
}
