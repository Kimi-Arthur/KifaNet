using Kifa.Service;

namespace Kifa.Cloud.Telegram;

public class TelegramStorageCell : DataModel, WithModelId {
    public static string ModelId => "telegram/cells";

    public static KifaServiceClient<TelegramStorageCell> Client { get; set; } =
        new KifaServiceRestClient<TelegramStorageCell>();

    #region public late long ApiId { get; set; }

    int? apiId;

    public int ApiId {
        get => Late.Get(apiId);
        set => Late.Set(ref apiId, value);
    }

    #endregion

    #region public late string ApiHash { get; set; }

    string? apiHash;

    public string ApiHash {
        get => Late.Get(apiHash);
        set => Late.Set(ref apiHash, value);
    }

    #endregion

    #region public late string Phone { get; set; }

    string? phone;

    public string Phone {
        get => Late.Get(phone);
        set => Late.Set(ref phone, value);
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
