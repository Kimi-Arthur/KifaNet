using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Cloud.MegaNz;
using Pimix.IO;
using Pimix.Service;

namespace PimixTest.Cloud.MegaNz {
    [TestClass]
    public class MegaNzStorageClientTests {
        readonly string FileSHA256 =
            "68EB5DFB2935868A17EEDDB315FBF6682243D29C1C1A20CC06BD25627F596285";

        public static string PimixServerApiAddress { get; set; } = "http://www.pimix.tk/api";

        [TestMethod]
        public void ExistsTest() {
            var client = GetStorageClient();

            Assert.IsTrue(client.Exists("/Test/2010-11-25.bin"));
            Assert.IsFalse(client.Exists("/Test/2015-11-25.bin"));
            Assert.IsFalse(client.Exists("/Test/non/2015-11-25.bin"));
        }

        [TestMethod]
        public void DownloadTest() {
            var client = GetStorageClient();

            using (var s = client.OpenRead("/Test/2010-11-25.bin")) {
                Assert.AreEqual(FileSHA256,
                    FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
            }
        }

        [TestMethod]
        public void UploadTest() {
            var client = GetStorageClient();

            client.Write("/Test/new/upload.bin", File.OpenRead("data.bin"));

            Thread.Sleep(TimeSpan.FromSeconds(1));

            using (var s = client.OpenRead("/Test/new/upload.bin")) {
                Assert.AreEqual(FileSHA256,
                    FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
            }

            client.Delete("/Test/new/upload.bin");
            client.Delete("/Test/new/");
        }

        [TestMethod]
        public void CopyTest() {
            var client = GetStorageClient();

            client.Copy("/Test/2010-11-25.bin", "/Test/2010-11-25.bin_bak");
            using (var s = client.OpenRead("/Test/2010-11-25.bin_bak")) {
                Assert.AreEqual(FileSHA256,
                    FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
            }

            client.Delete("/Test/2010-11-25.bin_bak");
        }

        [TestMethod]
        public void MoveTest() {
            var client = GetStorageClient();

            client.Copy("/Test/2010-11-25.bin", "/Test/2010-11-25.bin_1");
            Assert.IsTrue(client.Exists("/Test/2010-11-25.bin_1"));
            Assert.IsFalse(client.Exists("/Test/2010-11-25.bin_2"));

            client.Move("/Test/2010-11-25.bin_1", "/Test/2010-11-25.bin_2");
            Assert.IsFalse(client.Exists("/Test/2010-11-25.bin_1"));
            Assert.IsTrue(client.Exists("/Test/2010-11-25.bin_2"));

            using (var s = client.OpenRead("/Test/2010-11-25.bin_2")) {
                Assert.AreEqual(FileSHA256,
                    FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
            }

            client.Delete("/Test/2010-11-25.bin_2");
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext ctx) {
            PimixServiceRestClient.PimixServerApiAddress = PimixServerApiAddress;
            DataCleanup();
        }

        [ClassCleanup]
        public static void ClassClenaup() => DataCleanup();

        static void DataCleanup() {
            var client = GetStorageClient();

            var files = new[] {
                "/Test/2010-11-25.bin_bak", "/Test/2010-11-25.bin_1", "/Test/2010-11-25.bin_2", "/Test/new/upload.bin",
                "/Test/new/"
            };

            foreach (var f in files) {
                try {
                    client.Delete(f);
                } catch (Exception) {
                }
            }
        }

        static MegaNzStorageClient GetStorageClient()
            => new MegaNzStorageClient {
                AccountId = "test"
            };
    }
}
