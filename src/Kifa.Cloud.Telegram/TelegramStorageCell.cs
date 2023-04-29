using System;
using System.Threading.Tasks;
using Kifa.Service;
using Newtonsoft.Json;
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

    Client? telegramClient;

    [JsonIgnore]
    public Client TelegramClient {
        get {
            if (telegramClient == null) {
                (telegramClient, SessionId) = Account.Data.Checked().CreateClient();
                KeepSessionRefreshed(SessionId);
            }

            return telegramClient;
        }
    }

    [JsonIgnore]
    public int SessionId { get; set; }

    InputPeer? channel;

    [JsonIgnore]
    public InputPeer Channel
        => channel ??= Retry
            .Run(() => TelegramClient.Messages_GetAllChats().GetAwaiter().GetResult(),
                TelegramStorageClient.HandleFloodException).chats[long.Parse(ChannelId)].Checked();

    async Task KeepSessionRefreshed(int sessionId) {
        while (true) {
            if (!TelegramAccount.Client.RenewSession(Account.Id, sessionId).IsAcceptable) {
                break;
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    public void ResetClient() {
        TelegramAccount.Client.ReleaseSession(Account.Id, SessionId);
        telegramClient = null;
        channel = null;
    }
}
