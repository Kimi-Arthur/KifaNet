﻿using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Kifa.Cloud.BaiduCloud;
using Kifa.IO;
using Kifa.Service;

namespace Kifa.Cloud.BaiduCloud.Tests {
    [TestClass]
    public class BaiduCloudStorageClientTests {
        const string FileSha256 =
            "68EB5DFB2935868A17EEDDB315FBF6682243D29C1C1A20CC06BD25627F596285";

        const string BigFileSha256 =
            "C15129F8F953AF57948FBC05863C42E16A8362BD5AEC9F88C566998D1CED723A";

        public static string PimixServerApiAddress { get; set; } = "http://www.pimix.tk/api";

        [TestMethod]
        public void DownloadTest() {
            var client = GetStorageClient();

            using var s = client.OpenRead("/Test/2010-11-25.bin");
            Assert.AreEqual(FileSha256,
                FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
        }

        [TestMethod]
        public void CopyTest() {
            var client = GetStorageClient();

            client.Copy("/Test/2010-11-25.bin", "/Test/2010-11-25.bin_bak");
            using (var s = client.OpenRead("/Test/2010-11-25.bin_bak")) {
                Assert.AreEqual(FileSha256,
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
                Assert.AreEqual(FileSha256,
                    FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
            }

            client.Delete("/Test/2010-11-25.bin_2");
        }

        [TestMethod]
        public void UploadRapidAndRemoveTest() {
            var client = GetStorageClient();

            client.UploadStreamRapid("/Test/rapid.bin",
                new FileInformation {
                    Size = 1048576,
                    Md5 = "3DD3601B968AEBB08C6FD3E1A66D22C3",
                    Adler32 = "6B9CF2BA",
                    SliceMd5 = "70C2358C662FB2A7EAC51902FA398BA2"
                });

            Thread.Sleep(TimeSpan.FromSeconds(1));

            using (var s = client.OpenRead("/Test/rapid.bin")) {
                Assert.AreEqual(FileSha256,
                    FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
            }

            client.Delete("/Test/rapid.bin");
        }

        [TestMethod]
        public void UploadByBlockTest() {
            var client = GetStorageClient();
            var data = new byte[34 << 20];
            File.OpenRead("data.bin").Read(data, 0, 1 << 20);
            for (var i = 1; i < 34; ++i) {
                Array.Copy(data, 0, data, i << 20, 1 << 20);
            }

            var dataStream = new MemoryStream(data);

            client.Write("/Test/block.bin", dataStream);

            Thread.Sleep(TimeSpan.FromSeconds(1));

            using (var s = client.OpenRead("/Test/block.bin")) {
                Assert.AreEqual(BigFileSha256,
                    FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
            }

            client.Delete("/Test/block.bin");
        }

        [TestMethod]
        public void UploadDirectTest() {
            var client = GetStorageClient();

            client.Write("/Test/direct.bin",
                File.OpenRead("data.bin"));

            Thread.Sleep(TimeSpan.FromSeconds(1));

            using (var s = client.OpenRead("/Test/direct.bin")) {
                Assert.AreEqual(FileSha256,
                    FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
            }

            client.Delete("/Test/direct.bin");
        }

        [TestMethod]
        public void ExistsTest() {
            var client = GetStorageClient();

            Assert.IsTrue(client.Exists("/Test/2010-11-25.bin"));
            Assert.IsFalse(client.Exists("/Test/2015-11-25.bin"));
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext ctx) {
            KifaServiceRestClient.ServerAddress = PimixServerApiAddress;
            DataCleanup();
        }

        [ClassCleanup]
        public static void ClassClenaup() => DataCleanup();

        static void DataCleanup() {
            var client = GetStorageClient();

            var files = new[] {
                "/Test/2010-11-25.bin_bak", "/Test/2010-11-25.bin_1", "/Test/2010-11-25.bin_2", "/Test/rapid.bin",
                "/Test/block.bin", "/Test/direct.bin"
            };

            foreach (var f in files) {
                try {
                    client.Delete(f);
                } catch (Exception) {
                }
            }
        }

        static BaiduCloudStorageClient GetStorageClient()
            => new BaiduCloudStorageClient {
                AccountId = "PimixT"
            };
    }
}