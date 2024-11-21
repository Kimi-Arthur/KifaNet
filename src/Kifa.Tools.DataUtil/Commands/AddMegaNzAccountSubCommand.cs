using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Kifa.Cloud.MegaNz;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil.Commands;

public class AddMegaNzAccountSubCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // Example command: datax add -t swisscom/accounts p060
    public static void CreateMegaNzAccounts(IEnumerable<string> specs) {
        foreach (var account in specs.SelectMany(ExpandAccounts)) {
            account.Register();
            UploadAccount(account);
        }
        //     var swisscomProcessor = new ConcurrentProcessor<KifaActionResult> {
        //         Validator = KifaActionResult.ActionValidator,
        //         TotalRetryCount = 3,
        //         CooldownDuration = TimeSpan.Zero
        //     };
        //
        //     var myCloudProcessor = new ConcurrentProcessor<KifaActionResult> {
        //         Validator = KifaActionResult.ActionValidator,
        //         TotalRetryCount = 5,
        //         CooldownDuration = TimeSpan.FromSeconds(10)
        //     };
        //
        //     foreach (var account in specs.SelectMany(ExpandAccounts)) {
        //         swisscomProcessor.Add(() => KifaActionResult.FromAction(() => {
        //             var quota = SwisscomAccountQuota.Client.Get(account.Id, true);
        //             if (quota?.TotalQuota > 0) {
        //                 return new KifaActionResult {
        //                     Status = KifaActionStatus.OK,
        //                     Message = "Account already registered."
        //                 };
        //             }
        //
        //             var status = account.Register();
        //             switch (status) {
        //                 case AccountRegistrationStatus.Registered:
        //                     return UploadAccount(account);
        //
        //                 case AccountRegistrationStatus.OnlySwisscom:
        //                     myCloudProcessor.Add(() => KifaActionResult.FromAction(() => {
        //                         var x = account.RegisterMyCloud();
        //                         return !x.IsAcceptable ? x : UploadAccount(account);
        //                     }));
        //
        //                     return new KifaActionResult {
        //                         Status = KifaActionStatus.OK
        //                     };
        //
        //                 case AccountRegistrationStatus.NotRegistered:
        //                 default:
        //                     return new KifaActionResult {
        //                         Status = KifaActionStatus.Error,
        //                         Message = "Account NOT registered."
        //                     };
        //             }
        //         }));
        //     }
        //
        //     swisscomProcessor.Start(ParallelThreads);
        //     myCloudProcessor.Start(ParallelThreads * 2);
        //
        //     swisscomProcessor.Stop();
        //     myCloudProcessor.Stop();
    }

    static KifaActionResult UploadAccount(MegaNzAccount account) {
        MegaNzAccount.Client.Set(account);
        var accountFromServer = MegaNzAccount.Client.Get(account.Id);
        if (accountFromServer == null) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Failed to get the account ({account.Id}) from server."
            };
        }

        return new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message = $"Successfully uploaded MegaNzAccount as: {accountFromServer}"
        };
    }

    static IEnumerable<MegaNzAccount> ExpandAccounts(string spec) {
        var type = spec[..1];
        var count = 1 << 4 * (5 - spec.Length);
        var number = int.Parse(spec[1..], NumberStyles.HexNumber) * count;
        for (int i = 0; i < count; i++) {
            var id = $"{type}{number + i:x4}";
            yield return new MegaNzAccount {
                Id = id,
                Username = GetMail(id),
                Password = MegaNzAccount.DefaultPassword
            };
        }
    }

    static readonly string BaseEmail = "kchbot+{id}@gmail.com";

    static string GetMail(string id)
        => BaseEmail.Format(new() {
            ["id"] = id
        });
}
