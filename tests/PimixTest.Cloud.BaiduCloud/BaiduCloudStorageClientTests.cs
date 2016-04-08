using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Cloud.BaiduCloud;
using Pimix.IO;

namespace PimixTest.Cloud.BaiduCloud
{
    [TestClass]
    public class BaiduCloudStorageClientTests
    {
        public static string PimixServerApiAddress { get; set; } = "http://pimix.cloudapp.net/api";

        string FileSHA256 = "68EB5DFB2935868A17EEDDB315FBF6682243D29C1C1A20CC06BD25627F596285";

        [TestMethod]
        public void DownloadTest()
        {
            var client = new BaiduCloudStorageClient() { AccountId = "PimixT" };
            using (var s = client.OpenRead("/Test/2010-11-25.bin"))
            {
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }
        }

        [TestMethod]
        public void UploadRapidAndRemoveTest()
        {
            var client = new BaiduCloudStorageClient() { AccountId = "PimixT" };

            client.Write(
                "/Test/rapid.bin",
                fileInformation: new FileInformation
                {
                    Size = 1048576,
                    MD5 = "3DD3601B968AEBB08C6FD3E1A66D22C3",
                    Adler32 = "6B9CF2BA",
                    SliceMD5 = "70C2358C662FB2A7EAC51902FA398BA2"
                },
                match: true);

            Thread.Sleep(TimeSpan.FromSeconds(1));

            using (var s = client.OpenRead("/Test/rapid.bin"))
            {
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }

            client.Delete("/Test/rapid.bin");
        }

        [TestMethod]
        public void UploadByBlockTest()
        {
            var client = new BaiduCloudStorageClient()
            {
                AccountId = "PimixT",
                BlockInfo = new List<int> { 128 << 10 }
            };

            client.Write(
                "/Test/block.bin",
                File.OpenRead("data.bin"),
                match: false
            );

            Thread.Sleep(TimeSpan.FromSeconds(1));

            using (var s = client.OpenRead("/Test/block.bin"))
            {
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }

            client.Delete("/Test/block.bin");
        }

        [TestMethod]
        public void UploadDirectTest()
        {
            var client = new BaiduCloudStorageClient() { AccountId = "PimixT" };

            client.Write(
                "/Test/direct.bin",
                File.OpenRead("data.bin"),
                match: false
            );

            Thread.Sleep(TimeSpan.FromSeconds(1));

            using (var s = client.OpenRead("/Test/direct.bin"))
            {
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }

            client.Delete("/Test/direct.bin");
        }

        [TestMethod]
        public void ExistsTest()
        {
            var client = new BaiduCloudStorageClient() { AccountId = "PimixT" };

            Assert.IsTrue(client.Exists("/Test/2010-11-25.bin"));
            Assert.IsFalse(client.Exists("/Test/2015-11-25.bin"));
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext ctx)
        {
            BaiduCloudConfig.PimixServerApiAddress = PimixServerApiAddress;
            BaiduCloudStorageClient.Config = BaiduCloudConfig.Get("baidu_cloud");

            DataCleanup();
        }

        [ClassCleanup]
        public static void ClassClenaup()
            => DataCleanup();

        static void DataCleanup()
        {
            var client = new BaiduCloudStorageClient() { AccountId = "PimixT" };

            try
            {
                client.Delete("/Test/rapid.bin");
            }
            catch (Exception)
            {
            }

            try
            {
                client.Delete("/Test/block.bin");
            }
            catch (Exception)
            {
            }

            try
            {
                client.Delete("/Test/direct.bin");
            }
            catch (Exception)
            {
            }
        }
    }
}
