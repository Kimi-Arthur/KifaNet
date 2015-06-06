using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Cloud.Baidu;
using Pimix.Service;
using Pimix.Storage;

namespace PimixTest.Cloud.Baidu
{
    [TestClass]
    public class StorageClientTests
    {
        public string PimixServerApiAddress { get; set; } = "http://test.pimix.org/api";

        [TestMethod]
        public void DownloadTest()
        {
            var client = new StorageClient() { AccountId = "PimixT" };
            using (var s = client.GetDownloadStream("Test/2010-11-25.bin"))
            {
                Assert.AreEqual(0x39, s.ReadByte());
                Assert.AreEqual(0x6c, s.ReadByte());
            }
        }

        [TestMethod]
        public void UploadRapidTest()
        {
            var client = new StorageClient() { AccountId = "PimixT" };

            client.UploadStream("Test/rapid.bin",
                fileInformation: new FileInformation
                {
                    Size = 1048576,
                    MD5 = "3DD3601B968AEBB08C6FD3E1A66D22C3",
                    CRC32 = "6B9CF2BA",
                    SliceMD5 = "70C2358C662FB2A7EAC51902FA398BA2"
                });

            using (var s = client.GetDownloadStream("Test/rapid.bin"))
            {
                Assert.AreEqual(0x39, s.ReadByte());
                Assert.AreEqual(0x6c, s.ReadByte());
            }
        }

        [TestInitialize]
        public void Initialize()
        {
            DataModel.PimixServerApiAddress = PimixServerApiAddress;
            StorageClient.Config = DataModel.Get<BaiduCloudConfig>("baidu_cloud");
        }
    }
}
