using System.IO;
using Kifa.Service;
using TL;
using WTelegram;

namespace Kifa.Cloud.Telegram;

public class TelegramStorageCell : DataModel, WithModelId<TelegramStorageCell> {
    public static string ModelId => "telegram/cells";

    public static KifaServiceClient<TelegramStorageCell> Client { get; set; } =
        new KifaServiceRestClient<TelegramStorageCell>();

    #region public late Link<TelegramAccount> Account { get; set; }

    Link<TelegramAccount>? account;

    public Link<TelegramAccount> Account {
        get => Late.Get(account);
        set => Late.Set(ref account, value);
    }

    #endregion

    #region public late string ChannelId { get; set; }

    string? channelId;

    public string ChannelId {
        get => Late.Get(channelId);
        set => Late.Set(ref channelId, value);
    }

    #endregion

    bool toRefresh = true;
    Client? telegramClient;

    public Client TelegramClient {
        get {
            telegramClient ??= Account.Data.Checked().GetClient();
            if (toRefresh) {
                RefreshClient();
                toRefresh = false;
            }

            return telegramClient;
        }
    }

    public InputPeer Channel
        => Retry.Run(() => TelegramClient.Messages_GetAllChats().GetAwaiter().GetResult(),
            TelegramStorageClient.HandleFloodException).chats[long.Parse(ChannelId)].Checked();

    public void ResetClient() {
        toRefresh = true;
    }

    void RefreshClient() {
        var result =
            Retry.Run(
                () => telegramClient.Checked().Login(Account.Data.Checked().Phone).GetAwaiter()
                    .GetResult(), TelegramStorageClient.HandleFloodException);
        if (result != null) {
            throw new DriveNotFoundException(
                $"Telegram drive {Id} is not accessible. Requesting {result}.");
        }
    }
}
