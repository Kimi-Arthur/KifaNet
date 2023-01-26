using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Kifa.Cloud.Swisscom;
using Kifa.Service;

namespace Kifa.Tools.DataUtil.Commands;

public partial class AddCommand {
    void CreateSwisscomAccounts(IEnumerable<string> specs) {
        // var myCloud = new ConcurrentQueue<SwisscomAccount>();

        specs.SelectMany(ExpandAccounts).AsParallel().WithDegreeOfParallelism(ParallelThreads)
            .ForAll(account => ExecuteItem(account.Id, () => {
                var quota = SwisscomAccountQuota.Client.Get(account.Id, true);
                if (quota?.TotalQuota > 0) {
                    return new KifaActionResult {
                        Status = KifaActionStatus.OK,
                        Message = "Account already registered."
                    };
                }

                var status = account.Register();
                switch (status) {
                    case AccountRegistrationStatus.Registered:
                        return UploadAccount(account);

                    case AccountRegistrationStatus.OnlySwisscom:
                        // myCloud.Enqueue(account);
                        // return new KifaActionResult {
                        //     Status = KifaActionStatus.Pending,
                        //     Message = "Account needs to be registered for myCloud."
                        // };
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                        account.RegisterMyCloud();
                        return UploadAccount(account);
                    case AccountRegistrationStatus.NotRegistered:
                    default:
                        return new KifaActionResult {
                            Status = KifaActionStatus.Error,
                            Message = "Account NOT registered."
                        };
                }
            }));
    }

    static KifaActionResult UploadAccount(SwisscomAccount account) {
        SwisscomAccount.Client.Set(account);
        var accountFromServer = SwisscomAccount.Client.Get(account.Id);
        if (accountFromServer == null) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Failed to get the account ({account.Id}) from server."
            };
        }

        var quota = SwisscomAccountQuota.Client.Get(accountFromServer.Id);
        return new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message = $"Successfully registered account {accountFromServer}\nwith quota {quota}"
        };
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
