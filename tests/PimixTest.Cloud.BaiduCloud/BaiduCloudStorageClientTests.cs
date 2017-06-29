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
            var client = GetStorageClient();

            using (var s = client.OpenRead("/Test/2010-11-25.bin"))
            {
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }
        }

        [TestMethod]
        public void CopyTest()
        {
            var client = GetStorageClient();

            client.Copy("/Test/2010-11-25.bin", "/Test/2010-11-25.bin_bak");
            using (var s = client.OpenRead("/Test/2010-11-25.bin_bak"))
            {
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }

            client.Delete("/Test/2010-11-25.bin_bak");
        }

        [TestMethod]
        public void MoveTest()
        {
            var client = GetStorageClient();

            client.Copy("/Test/2010-11-25.bin", "/Test/2010-11-25.bin_1");
            Assert.IsTrue(client.Exists("/Test/2010-11-25.bin_1"));
            Assert.IsFalse(client.Exists("/Test/2010-11-25.bin_2"));

            client.Move("/Test/2010-11-25.bin_1", "/Test/2010-11-25.bin_2");
            Assert.IsFalse(client.Exists("/Test/2010-11-25.bin_1"));
            Assert.IsTrue(client.Exists("/Test/2010-11-25.bin_2"));

            using (var s = client.OpenRead("/Test/2010-11-25.bin_2"))
            {
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }

            client.Delete("/Test/2010-11-25.bin_2");
        }

        [TestMethod]
        public void UploadRapidAndRemoveTest()
        {
            var client = GetStorageClient();

            client.UploadStreamRapid(
                "/Test/rapid.bin",
                fileInformation: new FileInformation
                {
                    Size = 1048576,
                    MD5 = "3DD3601B968AEBB08C6FD3E1A66D22C3",
                    Adler32 = "6B9CF2BA",
                    SliceMD5 = "70C2358C662FB2A7EAC51902FA398BA2"
                });

            Thread.Sleep(TimeSpan.FromSeconds(1));

            using (var s = client.OpenRead("/Test/rapid.bin"))
            {
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }

            client.Delete("/Test/rapid.bin");
        }

        //[TestMethod]
        //public void UploadByBlockTest()
        //{
        //    var client = GetStorageClient();
        //    client.BlockInfo = new List<int> { 128 << 10 };

        //    client.Write(
        //        "/Test/block.bin",
        //        File.OpenRead("data.bin"),
        //        match: false
        //    );

        //    Thread.Sleep(TimeSpan.FromSeconds(1));

        //    using (var s = client.OpenRead("/Test/block.bin"))
        //    {
        //        Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
        //    }

        //    client.Delete("/Test/block.bin");
        //}

        [TestMethod]
        public void UploadDirectTest()
        {
            var client = GetStorageClient();

            client.Write(
                "/Test/direct.bin",
                File.OpenRead("data.bin"));

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
            var client = GetStorageClient();

            Assert.IsTrue(client.Exists("/Test/2010-11-25.bin"));
            Assert.IsFalse(client.Exists("/Test/2015-11-25.bin"));
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext ctx)
        {
            BaiduCloudConfig.PimixServerApiAddress = PimixServerApiAddress;
            BaiduCloudStorageClient.Config = BaiduCloudConfig.Get("default");

            DataCleanup();
        }

        [ClassCleanup]
        public static void ClassClenaup()
            => DataCleanup();

        static void DataCleanup()
        {
            var client = GetStorageClient();

            var files = new string[]
            {
                "/Test/2010-11-25.bin_bak",
                "/Test/2010-11-25.bin_1",
                "/Test/2010-11-25.bin_2",
                "/Test/rapid.bin",
                "/Test/block.bin",
                "/Test/direct.bin"
            };

            foreach (var f in files)
            {
                try
                {
                    client.Delete(f);
                }
                catch (Exception)
                {
                }
            }
        }

        static BaiduCloudStorageClient GetStorageClient()
            => new BaiduCloudStorageClient() { AccountId = "PimixT" };
    }
}
