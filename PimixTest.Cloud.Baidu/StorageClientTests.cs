using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Cloud.Baidu;
using Pimix.Service;

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

        [TestInitialize]
        public void Initialize()
        {
            DataModel.PimixServerApiAddress = PimixServerApiAddress;
            StorageClient.Config = DataModel.Get<Config>("baidu_cloud");
        }
    }
}
