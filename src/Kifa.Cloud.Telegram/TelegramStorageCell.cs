using System;
using System.Threading;
using Kifa.IO;
using Kifa.Service;
using NLog;

namespace Kifa.Cloud.Telegram;

public class TelegramStorageCell : DataModel, WithModelId<TelegramStorageCell> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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

    static readonly TimeSpan SessionWaitInterval = TimeSpan.FromSeconds(10);

    public TelegramCellClient CreateClient(TelegramSessionType type) {
        while (true) {
            try {
                Logger.Debug($"Obtaining {type} session for account {Account.Id}...");
                var response = TelegramAccount.Client.ObtainSession(Account.Id, type);
                if (response.Status != KifaActionStatus.OK) {
                    throw new KifaActionFailedException(response);
                }

                Logger.Debug(
                    $"Obtained {type} session {response.Value.Checked().Id} for account {Account.Id}.");
                return new TelegramCellClient(Account.Data.Checked(), ChannelId,
                    response.Value.Checked(), type);
            } catch (KifaActionFailedException ex) {
                if (ex.ActionResult.IsRetryable) {
                    Logger.Warn(ex,
                        $"Failed to obtain {type} session for account {Account.Id}: {ex.ActionResult.Message}. Retrying...");
                    if (ex.ActionResult.Status != KifaActionStatus.Pending) {
                        Thread.Sleep(SessionWaitInterval);
                    }

                    continue;
                }

                throw new InsufficientStorageException(
                    $"Failed to locate a session to use: {ex.ActionResult.Message}");
            }
        }
    }
}
