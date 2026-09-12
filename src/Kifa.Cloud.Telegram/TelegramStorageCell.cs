using Kifa.IO;
using Kifa.Service;

namespace Kifa.Cloud.Telegram;

public class TelegramStorageCell : DataModel, WithModelId<TelegramStorageCell> {
    public static string ModelId => "telegram/cells";

    public static KifaServiceClient<TelegramStorageCell> Client { get; set; } =
        new KifaServiceRestClient<TelegramStorageCell>();

    public Link<TelegramAccount> Account {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public string ChannelId {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public TelegramCellClient CreateClient() {
        var response = TelegramAccount.Client.ObtainSession(Account.Id);
        if (response.Status != KifaActionStatus.OK) {
            throw new InsufficientStorageException(
                $"Failed to locate a session to use: {response.Message}");
        }

        return new TelegramCellClient(Account, ChannelId, response.Value.Checked());
    }
}
