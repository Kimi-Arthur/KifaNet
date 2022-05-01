using System.Globalization;
using System.Text;

namespace Kifa.Tools.DataUtil.Commands;

public partial class AddCommand {
    void CreateSwisscomAccounts(string spec) {
        var type = spec[..1];
        var count = 1 << 4 * (5 - spec.Length);
        var number = int.Parse(spec[1..], NumberStyles.HexNumber) * count;
        for (int i = 0; i < count; i++) {
            logger.Info($"{type}{number + i:x4}: {GetMail(type, number + i)}");
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
