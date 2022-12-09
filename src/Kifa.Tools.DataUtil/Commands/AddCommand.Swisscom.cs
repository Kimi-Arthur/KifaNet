using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Kifa.Cloud.Swisscom;

namespace Kifa.Tools.DataUtil.Commands;

public partial class AddCommand {
    void CreateSwisscomAccounts(IEnumerable<string> specs) {
        foreach (var account in specs.SelectMany(GetAccounts)) {
            account.Register();
            SwisscomAccount.Client.Set(account);
            var accountFromServer = SwisscomAccount.Client.Get(account.Id);
            if (accountFromServer == null) {
                Logger.Error($"Failed to get the account ({account.Id}) from server.");
                continue;
            }

            var quota = SwisscomAccountQuota.Client.Get(accountFromServer.Id);
            Logger.Info($"Created: {accountFromServer}");
            Logger.Info($"with quota: {quota}");
        }
    }

    IEnumerable<SwisscomAccount> GetAccounts(string spec) {
        var type = spec[..1];
        var count = 1 << 4 * (5 - spec.Length);
        var number = int.Parse(spec[1..], NumberStyles.HexNumber) * count;
        for (int i = 0; i < count; i++) {
            yield return new SwisscomAccount {
                Id = $"{type}{number + i:x4}",
                Username = GetMail(type, number + i),
                Password = SwisscomAccount.DefaultPassword
            };
        }
    }

    string GetMail(string type, int number) {
        var sb = new StringBuilder(type);
        for (var i = 0; i < 16; i++) {
            if (number % 2 != 0) {
                sb.Append('.');
            }

            sb.Append($"{i:x}");
            number >>= 1;
        }

        return sb.Append("@gmail.com").ToString();
    }
}
