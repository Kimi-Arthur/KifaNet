using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using Kifa.Configs;
using Kifa.IO;
using Xunit;

namespace Kifa.Cloud.BaiduCloud.Tests;

public class BaiduCloudStorageClientTests : IDisposable {
    const string FileSha256 = "68EB5DFB2935868A17EEDDB315FBF6682243D29C1C1A20CC06BD25627F596285";

    const string BigFileSha256 = "C15129F8F953AF57948FBC05863C42E16A8362BD5AEC9F88C566998D1CED723A";

    public BaiduCloudStorageClientTests() {
        AppDomain.CurrentDomain.AssemblyLoad += (sender, eventArgs)
            => KifaConfigs.LoadFromSystemConfigs(eventArgs.LoadedAssembly);
        KifaConfigs.LoadFromSystemConfigs();

        DataCleanup();
    }

    [Fact]
    public void DownloadTest() {
        var client = GetStorageClient();

        using var s = client.OpenRead("/Test/2010-11-25.bin");
        FileInformation.GetInformation(s, FileProperties.Sha256).Sha256.Should().Be(FileSha256);
    }

    [Fact]
    public void CopyTest() {
        var client = GetStorageClient();

        client.Copy("/Test/2010-11-25.bin", "/Test/2010-11-25.bin_bak");
        using (var s = client.OpenRead("/Test/2010-11-25.bin_bak")) {
            FileInformation.GetInformation(s, FileProperties.Sha256).Sha256.Should().Be(FileSha256);
        }

        client.Delete("/Test/2010-11-25.bin_bak");
    }

    [Fact]
    public void MoveTest() {
        var client = GetStorageClient();

        client.Copy("/Test/2010-11-25.bin", "/Test/2010-11-25.bin_1");
        client.Exists("/Test/2010-11-25.bin_1").Should().BeTrue();
        client.Exists("/Test/2010-11-25.bin_2").Should().BeFalse();

        client.Move("/Test/2010-11-25.bin_1", "/Test/2010-11-25.bin_2");
        client.Exists("/Test/2010-11-25.bin_1").Should().BeFalse();
        client.Exists("/Test/2010-11-25.bin_2").Should().BeTrue();

        using (var s = client.OpenRead("/Test/2010-11-25.bin_2")) {
            FileInformation.GetInformation(s, FileProperties.Sha256).Sha256.Should().Be(FileSha256);
        }

        client.Delete("/Test/2010-11-25.bin_2");
    }

    [Fact]
    public void UploadRapidAndRemoveTest() {
        var client = GetStorageClient();

        client.UploadStreamRapid("/Test/rapid.bin", new FileInformation {
            Size = 1048576,
            Md5 = "3DD3601B968AEBB08C6FD3E1A66D22C3",
            Adler32 = "6B9CF2BA",
            SliceMd5 = "70C2358C662FB2A7EAC51902FA398BA2"
        });

        Thread.Sleep(TimeSpan.FromSeconds(1));

        using (var s = client.OpenRead("/Test/rapid.bin")) {
            FileInformation.GetInformation(s, FileProperties.Sha256).Sha256.Should().Be(FileSha256);
        }

        client.Delete("/Test/rapid.bin");
    }

    [Fact]
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
            FileInformation.GetInformation(s, FileProperties.Sha256).Sha256.Should()
                .Be(BigFileSha256);
        }

        client.Delete("/Test/block.bin");
    }

    [Fact]
    public void UploadDirectTest() {
        var client = GetStorageClient();

        client.Write("/Test/direct.bin", File.OpenRead("data.bin"));

        Thread.Sleep(TimeSpan.FromSeconds(1));

        using (var s = client.OpenRead("/Test/direct.bin")) {
            FileInformation.GetInformation(s, FileProperties.Sha256).Sha256.Should().Be(FileSha256);
        }

        client.Delete("/Test/direct.bin");
    }

    [Fact]
    public void ExistsTest() {
        var client = GetStorageClient();

        client.Exists("/Test/2010-11-25.bin").Should().BeTrue();
        client.Exists("/Test/2015-11-25.bin").Should().BeFalse();
    }

    public static void ClassClenaup() => DataCleanup();

    static void DataCleanup() {
        var client = GetStorageClient();

        var files = new[] {
            "/Test/2010-11-25.bin_bak",
            "/Test/2010-11-25.bin_1",
            "/Test/2010-11-25.bin_2",
            "/Test/rapid.bin",
            "/Test/block.bin",
            "/Test/direct.bin"
        };

        foreach (var f in files) {
            try {
                client.Delete(f);
            } catch (Exception) {
            }
        }
    }

    static BaiduCloudStorageClient GetStorageClient()
        => new() {
            AccountId = "PimixT"
        };

    public void Dispose() {
        DataCleanup();
    }
}
