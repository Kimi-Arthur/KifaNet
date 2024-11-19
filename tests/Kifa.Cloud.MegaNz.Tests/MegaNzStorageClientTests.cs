using System;
using System.IO;
using System.Threading;
using CG.Web.MegaApiClient;
using Kifa.Configs;
using Kifa.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Kifa.Cloud.MegaNz.Tests;

[TestClass]
public class MegaNzStorageClientTests {
    const string FileSha256 = "68EB5DFB2935868A17EEDDB315FBF6682243D29C1C1A20CC06BD25627F596285";

    [TestMethod]
    public void QuotaTest() {
        var account = MegaNzConfig.Client.Get("default").Checked().Accounts["test"];
        var client = new MegaApiClient();
        client.Login(account.Username, account.Password);
        var info = client.GetAccountInformation();
        Assert.AreEqual(53687091200, info.TotalQuota);
        Assert.AreEqual(1048576, info.UsedQuota);

        account = MegaNzConfig.Client.Get("default").Checked().Accounts["0"];
        client = new MegaApiClient();
        client.Login(account.Username, account.Password);
        info = client.GetAccountInformation();
        Assert.AreEqual(21474836480, info.TotalQuota);
        Assert.AreEqual(0, info.UsedQuota);
    }

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

        using var s = client.OpenRead("/Test/2010-11-25.bin");
        Assert.AreEqual(FileSha256,
            FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
    }

    [TestMethod]
    public void UploadTest() {
        var client = GetStorageClient();

        client.Write("/Test/new/upload.bin", File.OpenRead("data.bin"));

        Thread.Sleep(TimeSpan.FromSeconds(1));

        using (var s = client.OpenRead("/Test/new/upload.bin")) {
            Assert.AreEqual(FileSha256,
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

    [ClassInitialize]
    public static void ClassInitialize(TestContext ctx) {
        KifaConfigs.Init();
        DataCleanup();
    }

    [ClassCleanup]
    public static void ClassClenaup() => DataCleanup();

    static void DataCleanup() {
        var client = GetStorageClient();

        var files = new string[] {
            // "/Test/2010-11-25.bin_bak",
            // "/Test/2010-11-25.bin_1",
            // "/Test/2010-11-25.bin_2",
            // "/Test/new/upload.bin",
            // "/Test/new/"
        };

        foreach (var f in files) {
            try {
                client.Delete(f);
            } catch (Exception) {
            }
        }
    }

    static MegaNzStorageClient GetStorageClient()
        => new() {
            AccountId = "test"
        };
}
