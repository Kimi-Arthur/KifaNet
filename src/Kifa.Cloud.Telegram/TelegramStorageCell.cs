using Kifa.IO;
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

    TelegramSession? currentSession;
    TelegramCellClient? currentClient;

    public TelegramCellClient CreateClient() {
        var response = TelegramAccount.Client.ObtainSession(Account.Id, currentSession?.Id);
        if (response.Status != KifaActionStatus.OK) {
            throw new InsufficientStorageException(
                $"Failed to locate a session to use: {response.Message}");
        }

        var newSession = response.Response.Checked();

        if (newSession.Data != currentSession?.Data) {
            currentClient?.Dispose();
            currentSession = newSession;
            currentClient = new TelegramCellClient(Account, ChannelId, currentSession);
        }

        currentClient.Checked().Reserved = true;
        return currentClient.Checked();
    }
}
