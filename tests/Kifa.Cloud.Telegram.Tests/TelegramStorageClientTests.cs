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
        var cell = TelegramStorageCell.Client.Get("test").Checked();
        var account = cell.Account.Data.Checked();
        var client = new Client(account.ApiId, account.ApiHash,
            $"{TelegramStorageClient.SessionsFolder}/{account.Id}.session");

        Assert.Equal("verification_code", client.Login(account.Phone).Result);

        // Manually login with client.Login("xxxxx")

        Assert.Null(client.Login(account.Phone).Result);
    }

    [Fact]
    public void InnerClientEndToEndTest() {
        var (client, channel) = GetClient();

        using var data = File.OpenRead("data.bin");
        var part1 = new byte[PartSize];
        data.Read(part1).Should().Be(PartSize);
        var part2 = new byte[PartSize];
        data.Read(part2).Should().Be(PartSize);

        var fileId = Random.Shared.NextInt64();
        var fileName = $"/Test/{fileId.ToByteArray().ToHexString()}";

        // This line breaks searching.
        var searchResults = client.Messages_Search<InputMessagesFilterDocument>(channel, fileName)
            .Result;
        searchResults.Messages.Should().BeEmpty();

        client.Upload_SaveBigFilePart(fileId, 0, 2, part1).Result.Should().BeTrue();
        client.Upload_SaveBigFilePart(fileId, 1, 2, part2).Result.Should().BeTrue();
        var uploadResult = client.SendMediaAsync(channel, fileName, new InputFileBig {
            id = fileId,
            parts = 2,
            name = fileName.Split("/")[^1]
        }).Result;
        uploadResult.message.Should().Be(fileName);

        searchResults = client.Messages_Search<InputMessagesFilterDocument>(channel, fileName)
            .Result;
        searchResults.Messages.Should().HaveCount(1);
        var message = searchResults.Messages[0] as Message;
        var document = (message.media as MessageMediaDocument).document as Document;

        document.size.Should().Be(1 << 20);
        message.message.Should().Be(fileName);

        var downloadResult =
            client.Upload_GetFile(document.ToFileLocation(), limit: 1 << 20).Result as Upload_File;
        downloadResult.bytes.Should().HaveCount(1 << 20);
        var downloadData = new MemoryStream(downloadResult.bytes);
        FileInformation.GetInformation(downloadData, FileProperties.Sha256).Sha256.Should()
            .Be(FileSha256);

        var deleteResult = client.DeleteMessages(channel, message.id).Result;
        deleteResult.pts_count.Should().Be(1);

        searchResults = client.Messages_Search<InputMessagesFilterDocument>(channel, fileName)
            .Result;
        searchResults.Messages.Should().BeEmpty();
    }


    [Fact]
    public void EndToEndTest() {
        KifaConfigs.Init();
        var storageClient = TelegramStorageClient.Create("test");

        using var data = File.OpenRead("data.bin");

        var fileName = $"/Test/{Random.Shared.NextInt64().ToByteArray().ToHexString()}";

        storageClient.Write(fileName, data);

        storageClient.Exists(fileName).Should().BeTrue();

        FileInformation.GetInformation(storageClient.OpenRead(fileName), FileProperties.Sha256)
            .Sha256.Should().Be(FileSha256);

        storageClient.Delete(fileName);

        storageClient.Exists(fileName).Should().BeFalse();
    }

    static (Client Client, InputPeer channel) GetClient() {
        KifaConfigs.Init();
        var client = (TelegramStorageClient.Create("test") as TelegramStorageClient).Checked();

        return (client.Client, client.Channel);
    }
}
