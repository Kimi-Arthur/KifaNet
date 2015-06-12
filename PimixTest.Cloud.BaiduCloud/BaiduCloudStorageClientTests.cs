using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Cloud.BaiduCloud;
using Pimix.Service;
using Pimix.IO;

namespace PimixTest.Cloud.BaiduCloud
{
    [TestClass]
    public class BaiduCloudStorageClientTests
    {
        public static string PimixServerApiAddress { get; set; } = "http://test.pimix.org/api";

        string FileSHA256 = "68EB5DFB2935868A17EEDDB315FBF6682243D29C1C1A20CC06BD25627F596285";

        [TestMethod]
        public void DownloadTest()
        {
            var client = new BaiduCloudStorageClient() { AccountId = "PimixT" };
            using (var s = client.GetDownloadStream("Test/2010-11-25.bin"))
            {
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }
        }

        [TestMethod]
        public void UploadRapidAndRemoveTest()
        {
            var client = new BaiduCloudStorageClient() { AccountId = "PimixT" };

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
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }

            client.DeleteFile("Test/rapid.bin");
        }

        [TestMethod]
        public void UploadByBlockTest()
        {
            var client = new BaiduCloudStorageClient() { AccountId = "PimixT" };

            client.UploadStream(
                "Test/block.bin",
                File.OpenRead("data.bin"),
                false,
                new List<int> { 128 << 10 }
            );

            using (var s = client.GetDownloadStream("Test/block.bin"))
            {
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }

            client.DeleteFile("Test/block.bin");
        }

        [TestMethod]
        public void UploadDirectTest()
        {
            var client = new BaiduCloudStorageClient() { AccountId = "PimixT" };

            client.UploadStream(
                "Test/direct.bin",
                File.OpenRead("data.bin"),
                false
            );

            using (var s = client.GetDownloadStream("Test/direct.bin"))
            {
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }

            client.DeleteFile("Test/direct.bin");
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext ctx)
        {
            DataModel.PimixServerApiAddress = PimixServerApiAddress;
            BaiduCloudStorageClient.Config = DataModel.Get<BaiduCloudConfig>("baidu_cloud");

            DataCleanup();
        }

        [ClassCleanup]
        public static void ClassClenaup()
        {
            DataCleanup();
        }

        static void DataCleanup()
        {
            var client = new BaiduCloudStorageClient() { AccountId = "PimixT" };

            try
            {
                client.DeleteFile("Test/rapid.bin");
            }
            catch (Exception)
            {
            }

            try
            {
                client.DeleteFile("Test/block.bin");
            }
            catch (Exception)
            {
            }

            try
            {
                client.DeleteFile("Test/direct.bin");
            }
            catch (Exception)
            {
            }
        }
    }
}
