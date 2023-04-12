using System;
using System.Collections.Generic;
using System.IO;
using Kifa.Cloud.Telegram;
using WTelegram;

namespace Kifa.Tools.DataUtil.Commands;

public partial class AddCommand {
    void CreateTelegramAccount(IEnumerable<string> specs) {
        foreach (var accountSpec in specs) {
            var segments = accountSpec.Split(":");
            var accountId = segments[0];
            if (TelegramAccount.Client.Get(accountId) != null) {
                if (Confirm($"Account {accountId} already exists. Skip")) {
                    Console.WriteLine($"Skipped adding account {accountId}.");
                    continue;
                }

                Console.WriteLine($"Will overwrite account {accountId}.");
            }

            var phone = segments[1];
            var apiId = segments[2];
            var apiHash = segments[3];

            var account = new TelegramAccount {
                Id = accountId,
                Phone = phone,
                ApiId = int.Parse(apiId),
                ApiHash = apiHash
            };

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

            account.Session = sessionStream.ToArray();
            Logger.Info($"Successfully login with account {accountId}. Uploading...");
            TelegramAccount.Client.Update(account);
            Logger.Info($"Successfully uploaded account {accountId}.");
        }
    }
}
