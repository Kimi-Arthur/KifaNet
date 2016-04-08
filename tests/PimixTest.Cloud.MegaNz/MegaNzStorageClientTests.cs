using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Cloud.MegaNz;
using Pimix.IO;

namespace PimixTest.Cloud.MegaNz
{
    [TestClass]
    public class MegaNzStorageClientTests
    {
        public static string PimixServerApiAddress { get; set; } = "http://pimix.cloudapp.net/api";

        string FileSHA256 = "68EB5DFB2935868A17EEDDB315FBF6682243D29C1C1A20CC06BD25627F596285";

        [TestMethod]
        public void DownloadTest()
        {
            var client = new MegaNzStorageClient() { AccountId = "pimixserver+test@gmail.com" };
            using (var s = client.OpenRead("/Test/2010-11-25.bin"))
            {
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }
        }

        [TestMethod]
        public void ExistsTest()
        {
            var client = new MegaNzStorageClient() { AccountId = "pimixserver+test@gmail.com" };

            Assert.IsTrue(client.Exists("/Test/2010-11-25.bin"));
            Assert.IsFalse(client.Exists("/Test/2015-11-25.bin"));
            Assert.IsFalse(client.Exists("/Test/non/2015-11-25.bin"));
        }

        [TestMethod]
        public void UploadTest()
        {
            var client = new MegaNzStorageClient() { AccountId = "pimixserver+test@gmail.com" };

            client.Write(
                "/Test/new/upload.bin",
                File.OpenRead("data.bin"),
                match: false
            );

            Thread.Sleep(TimeSpan.FromSeconds(1));

            using (var s = client.OpenRead("/Test/new/upload.bin"))
            {
                Assert.AreEqual(FileSHA256, FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }

            client.Delete("/Test/new/upload.bin");
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext ctx)
        {
            MegaNzConfig.PimixServerApiAddress = PimixServerApiAddress;
            MegaNzStorageClient.Config = MegaNzConfig.Get("mega_nz");

            DataCleanup();
        }

        [ClassCleanup]
        public static void ClassClenaup()
            => DataCleanup();

        static void DataCleanup()
        {
            var client = new MegaNzStorageClient() { AccountId = "pimixserver+test@gmail.com" };

            try
            {
                client.Delete("/Test/new/upload.bin");
            }
            catch (Exception)
            {
            }
        }
    }
}