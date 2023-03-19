using Kifa.Configs;
using WTelegram;
using Xunit;

namespace Kifa.Cloud.Telegram.Tests;

public class StorageClientTests {
    [Fact]
    public void SetupSession() {
        KifaConfigs.Init();
        var client = new Client(TelegramStorageClient.ApiId, TelegramStorageClient.ApiHash,
            TelegramStorageClient.SessionFilePath);
        if (client.Login(TelegramStorageClient.Phone).Result == "verification_code") {
            client.Login("xxxxx");
        }

        Assert.Null(client.Login(TelegramStorageClient.Phone).Result);
    }
}
