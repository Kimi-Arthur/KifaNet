using System;
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

    TelegramSession? currentSession;
    TelegramCellClient? currentClient;

    public TelegramCellClient CreateClient() {
        var response = TelegramAccount.Client.ObtainSession(Account.Id, currentSession?.Id);
        if (response.Status != KifaActionStatus.OK) {
            throw new InsufficientStorageException(
                $"Failed to locate a session to use: {response.Message}");
        }

        var newSession = response.Response.Checked();

        if (!new ReadOnlySpan<byte>(newSession.Data).SequenceEqual(currentSession?.Data)) {
            currentClient?.Dispose();
            currentSession = newSession;
            currentClient = new TelegramCellClient(Account, ChannelId, currentSession);
        }

        currentSession.Checked().Id = newSession.Id;

        currentClient.Checked().Reserved = true;
        return currentClient.Checked();
    }
}
