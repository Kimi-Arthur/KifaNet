using Kifa.Service;

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
}