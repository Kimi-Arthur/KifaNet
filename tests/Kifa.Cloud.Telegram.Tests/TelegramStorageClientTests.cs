using System;
using System.IO;
using FluentAssertions;
using Kifa.Configs;
using Kifa.IO;
using TL;
using WTelegram;
using Xunit;

namespace Kifa.Cloud.Telegram.Tests;

public class TelegramStorageClientTests {
    const int PartSize = 1 << 19;
    const string FileSha256 = "68EB5DFB2935868A17EEDDB315FBF6682243D29C1C1A20CC06BD25627F596285";

    [Fact]
    // This test is used to setup accounts to be used as the login process is hard to integrate into
    // the service.
    public void SetupSessionTest() {
        KifaConfigs.Init();
        var cell = TelegramStorageCell.Client.Get("Test").Checked();
        var account = cell.Account.Data.Checked();
        var client = new Client(account.ApiId, account.ApiHash,
            $"{TelegramStorageClient.SessionsFolder}/{account.Id}.session");

        Assert.Equal("verification_code", client.Login(account.Phone).Result);

        // Manually login with client.Log("xxxxx")

        Assert.Null(client.Login(account.Phone).Result);
    }

    [Fact]
    public void UploadFileTest() {
        var (client, channel) = GetClient();

        using var data = File.OpenRead("data.bin");
        var part1 = new byte[PartSize];
        data.Read(part1).Should().Be(PartSize);
        var part2 = new byte[PartSize];
        data.Read(part2).Should().Be(PartSize);

        var fileId = Random.Shared.NextInt64();

        client.Upload_SaveBigFilePart(fileId, 0, 2, part1).Result.Should().BeTrue();
        client.Upload_SaveBigFilePart(fileId, 1, 2, part2).Result.Should().BeTrue();
        var result = client.SendMediaAsync(channel, "/Test/new/upload.bin", new InputFileBig {
            id = fileId,
            parts = 2,
            name = "/Test/new/upload.bin"
        }).Result;
        result.Should().NotBeNull();
    }

    [Fact]
    public void DownloadFileTest() {
        var (client, channel) = GetClient();
        var document = new Document {
            access_hash = -3921921075344262639,
            id = 6104841500644870434,
            file_reference = "010000000A64177A411E9A47FE32486F23459028E2DA6FB103".ParseHexString()
        };
        var result =
            client.Upload_GetFile(document.ToFileLocation(), limit: 1 << 20).Result as Upload_File;
        result.bytes.Should().HaveCount(1 << 20);
        var data = new MemoryStream(result.bytes);
        FileInformation.GetInformation(data, FileProperties.Sha256).Sha256.Should().Be(FileSha256);
    }

    [Fact]
    public void SearchTest() {
        var (client, channel) = GetClient();
        var results = client
            .Messages_Search<InputMessagesFilterDocument>(channel, "/Test/new/upload.bin").Result;
        results.Messages.Should().HaveCount(1);
    }

    static (Client Client, InputPeer channel) GetClient() {
        KifaConfigs.Init();
        var client = new TelegramStorageClient {
            CellId = "Test"
        };

        client.EnsureLoggedIn();

        return (client.Client.Checked(), client.Channel.Checked());
    }
}
