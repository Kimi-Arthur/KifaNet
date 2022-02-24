using System.Drawing;

namespace Kifa.Subtitle.Ass;

public static class AssFormatter {
    public static string ToAss(this Color color) {
        var alpha = color.A != 255 ? $"{255 - color.A:X2}" : "";
        return $"&H{alpha}{color.B:X2}{color.G:X2}{color.R:X2}";
    }

    public static Color ParseColor(string content) {
        var alpha = 255;
        if (content.Length == 10) {
            alpha = 255 - content.Substring(2, 2).ParseHexString()[0];
            content = content.Substring(2);
        }

        return Color.FromArgb(alpha, content.Substring(6, 2).ParseHexString()[0],
            content.Substring(4, 2).ParseHexString()[0],
            content.Substring(2, 2).ParseHexString()[0]);
    }

    public static string ToAss(this bool b) => b ? "-1" : "0";

    public static bool ParseBool(string content) => content != "0";
}
