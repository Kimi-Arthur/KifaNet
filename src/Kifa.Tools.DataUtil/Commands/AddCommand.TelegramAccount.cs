using System.Collections.Generic;
using System.IO;
using Kifa.Cloud.Telegram;
using Kifa.Service;
using WTelegram;

namespace Kifa.Tools.DataUtil.Commands;

public partial class AddCommand {
    void CreateTelegramAccount(IEnumerable<string> specs) {
        foreach (var accountSpec in specs) {
            var segments = accountSpec.Split(":");
            var accountId = segments[0];
            var phone = segments[1];
            var apiId = segments[2];
            var apiHash = segments[3];
            var sessionCount = int.Parse(segments[4]);

            var account = new TelegramAccount {
                Id = accountId,
                Phone = phone,
                ApiId = int.Parse(apiId),
                ApiHash = apiHash
            };

            TelegramAccount.Client.Update(account);
            Logger.Info($"Updated account config for {accountId}.");

            if (!Confirm($"Will continue to add {sessionCount} sessions to account {accountId}")) {
                continue;
            }

            for (var i = 0; i < sessionCount; i++) {
                var sessionStream = new MemoryStream();
                sessionStream.Write(account.Session);
                var client = new Client(account.ConfigProvider, sessionStream);

                var result = Retry.Run(() => client.Login(account.Phone).GetAwaiter().GetResult(),
                    TelegramStorageClient.HandleFloodException);
                while (result != null) {
                    var code = Confirm($"Telegram asked for {result}:", "");
                    result = Retry.Run(() => client.Login(code).GetAwaiter().GetResult(),
                        TelegramStorageClient.HandleFloodException);
                }

                Logger.LogResult(
                    TelegramAccount.Client.AddSession(accountId, sessionStream.ToArray()),
                    "add new session");
            }
        }
    }
}
