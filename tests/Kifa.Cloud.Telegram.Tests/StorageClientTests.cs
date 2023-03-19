using FluentAssertions;
using Kifa.Configs;
using TL;
using WTelegram;
using Xunit;

namespace Kifa.Cloud.Telegram.Tests;

public class StorageClientTests {
    [Fact]
    public void SetupSessionTest() {
        KifaConfigs.Init();
        var client = new Client(TelegramStorageClient.ApiId, TelegramStorageClient.ApiHash,
            TelegramStorageClient.SessionFilePath);
        if (client.Login(TelegramStorageClient.Phone).Result == "verification_code") {
            client.Login("xxxxx");
        }

        Assert.Null(client.Login(TelegramStorageClient.Phone).Result);
    }

    [Fact]
    public void DownloadFileTest() {
        var client = GetClient();
        var config = client.TLConfig;
        client.FilePartSize.Should().Be(10);
    }

    static Client GetClient() {
        KifaConfigs.Init();
        var client = new Client(TelegramStorageClient.ApiId, TelegramStorageClient.ApiHash,
            TelegramStorageClient.SessionFilePath);
        Assert.Null(client.Login(TelegramStorageClient.Phone).Result);
        return client;
    }
}
