using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using Kifa.Configs;
using Kifa.IO;
using Xunit;

namespace Kifa.Cloud.Google.Tests;

public class GoogleDriveStorageClientTests {
    public GoogleDriveStorageClientTests() {
        GetStorageClient().Delete("/Test/big.bin");
    }

    const string FileSHA256 = "68EB5DFB2935868A17EEDDB315FBF6682243D29C1C1A20CC06BD25627F596285";

    const string BigFileSHA256 = "C15129F8F953AF57948FBC05863C42E16A8362BD5AEC9F88C566998D1CED723A";

    static StorageClient GetStorageClient() {
        KifaConfigs.Init();

        return GoogleDriveStorageClient.Create("test");
    }

    [Fact]
    public void DownloadTest() {
        var client = GetStorageClient();

        using var s = client.OpenRead("/Test/2010-11-25.bin");
        Assert.Equal(FileSHA256, FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);

        // Test again for seekness.
        Assert.Equal(FileSHA256, FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
    }

    [Fact]
    public void ExistsTest() {
        var client = GetStorageClient();

        Assert.True(client.Exists("/Test/2010-11-25.bin"));
        Assert.False(client.Exists("/Test/2015-11-25.bin"));
        Assert.False(client.Exists("/Test/NoFolder/2015-11-25.bin"));
    }

    [Fact]
    public void ListTest() {
        var client = GetStorageClient();

        client.List("/Test").Should().HaveCountGreaterThan(1);
        client.List("/Test/Empty").Should().HaveCount(0);
        client.List("/Test/NoFolder").Should().HaveCount(0);
    }

    [Fact]
    public void UploadTest() {
        var client = GetStorageClient();
        var data = new byte[34 << 20];
        File.OpenRead("data.bin").Read(data, 0, 1 << 20);
        for (var i = 1; i < 34; ++i) {
            Array.Copy(data, 0, data, i << 20, 1 << 20);
        }

        var dataStream = new MemoryStream(data);

        client.Write("/Test/temp2/big.bin", dataStream);

        Thread.Sleep(TimeSpan.FromSeconds(1));

        using (var s = client.OpenRead("/Test/temp2/big.bin")) {
            Assert.Equal(BigFileSHA256,
                FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
        }

        client.Delete("/Test/temp2/big.bin");
    }

    [Fact]
    public void MoveTest() {
        var client = GetStorageClient();
        client.Delete("/Test/copy");

        Assert.False(client.Exists("/Test/copy/a/a.bin"));
        Assert.False(client.Exists("/Test/copy/b/b.bin"));
        client.Copy("/Test/2010-11-25.bin", "/Test/copy/a/a.bin");
        Assert.True(client.Exists("/Test/copy/a/a.bin"));
        Assert.False(client.Exists("/Test/copy/b/b.bin"));
        client.Move("/Test/copy/a/a.bin", "/Test/copy/b/b.bin");
        Assert.True(client.Exists("/Test/copy/b/b.bin"));
        Assert.False(client.Exists("/Test/copy/a/a.bin"));
    }
}
