using System.Collections.Generic;
using System.IO;
using Kifa.Cloud.Telegram;
using Kifa.Service;
using NLog;
using WTelegram;

namespace Kifa.Tools.DataUtil.Commands;

public partial class AddCommand {
    // Example command: datax add -t telegram/accounts Kimily:1
    void CreateTelegramAccount(IEnumerable<string> specs) {
        foreach (var accountSpec in specs) {
            var segments = accountSpec.Split(":");
            var accountId = segments[0];
            var sessionCount = int.Parse(segments[^1]);

            TelegramAccount account;
            if (segments.Length == 5) {
                var phone = segments[1];
                var apiId = segments[2];
                var apiHash = segments[3];
                account = new TelegramAccount {
                    Id = accountId,
                    Phone = phone,
                    ApiId = int.Parse(apiId),
                    ApiHash = apiHash
                };

                TelegramAccount.Client.Update(account);
                Logger.Info($"Updated account config for {accountId}.");
            } else {
                account = TelegramAccount.Client.Get(accountId).Checked();
            }

            if (!Confirm($"Will continue to add {sessionCount} sessions to account {accountId}")) {
                continue;
            }

            for (var i = 0; i < sessionCount; i++) {
                MemoryStream? sessionStream = null;
                Client? client = null;

                var result = Retry.Run(() => {
                    sessionStream = new MemoryStream();
                    client = new Client(account.ConfigProvider, sessionStream);
                    return client.Login(account.Phone);
                }, TelegramStorageClient.HandleFloodExceptionFunc).GetAwaiter().GetResult();

                if (client == null || sessionStream == null) {
                    Logger.Error("Failed to init session stream or client to login.");
                    continue;
                }

                while (result != null) {
                    var code = Confirm($"Telegram asked for {result}:", "");
                    result = Retry.Run(() => client.Login(code),
                        TelegramStorageClient.HandleFloodExceptionFunc).GetAwaiter().GetResult();
                }

                Logger.LogResult(
                    TelegramAccount.Client.AddSession(accountId, sessionStream.ToArray()),
                    "add new session", LogLevel.Info);
            }
        }
    }
}
