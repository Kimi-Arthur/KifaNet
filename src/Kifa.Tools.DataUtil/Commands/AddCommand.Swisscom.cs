using System.Globalization;
using System.Text;
using Kifa.Cloud.Swisscom;

namespace Kifa.Tools.DataUtil.Commands;

public partial class AddCommand {
    void CreateSwisscomAccounts(string spec) {
        var type = spec[..1];
        var count = 1 << 4 * (5 - spec.Length);
        var number = int.Parse(spec[1..], NumberStyles.HexNumber) * count;
        for (int i = 0; i < count; i++) {
            var account = new SwisscomAccount {
                Id = $"{type}{number + i:x4}",
                Username = GetMail(type, number + i),
                Password = SwisscomAccount.DefaultPassword
            };

            account.Register();
            account = SwisscomAccount.Client.Get(account.Id);
            var quota = SwisscomAccountQuota.Client.Get(account.Id);
            Logger.Info(account);
            Logger.Info(quota);
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
