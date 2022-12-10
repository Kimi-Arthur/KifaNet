using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Kifa.Cloud.Swisscom;
using Kifa.Service;

namespace Kifa.Tools.DataUtil.Commands;

public partial class AddCommand {
    void CreateSwisscomAccounts(IEnumerable<string> specs) {
        specs.SelectMany(ExpandAccounts).AsParallel().WithDegreeOfParallelism(ParallelThreads).ForEach(account
            => ExecuteItem(account.Id, () => {
                var quota = SwisscomAccountQuota.Client.Get(account.Id, true);
                if (quota?.TotalQuota > 0) {
                    return new KifaActionResult {
                        Status = KifaActionStatus.OK,
                        Message = "Account already registered."
                    };
                }

                account.Register();
                SwisscomAccount.Client.Set(account);
                var accountFromServer = SwisscomAccount.Client.Get(account.Id);
                if (accountFromServer == null) {
                    return new KifaActionResult {
                        Status = KifaActionStatus.Error,
                        Message = $"Failed to get the account ({account.Id}) from server."
                    };
                }

                quota = SwisscomAccountQuota.Client.Get(accountFromServer.Id);
                return new KifaActionResult {
                    Status = KifaActionStatus.OK,
                    Message =
                        $"Successfully registered account {accountFromServer}\nwith quota {quota}"
                };
            }));
    }

    IEnumerable<SwisscomAccount> ExpandAccounts(string spec) {
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
