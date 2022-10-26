using System;
using System.Drawing;

namespace Kifa.Subtitle.Ass;

public static class AssFormatter {
    public static string ToAss(this Color color) {
        var alpha = color.A != 255 ? $"{255 - color.A:X2}" : "";
        return $"&H{alpha}{color.B:X2}{color.G:X2}{color.R:X2}";
    }

    public static Color ParseColor(string content) {
        content = content.TrimStart('H', '&');
        var alpha = 255;
        if (content.Length == 8) {
            alpha = 255 - content[..2].ParseHexString()[0];
            content = content[2..];
        }

        if (content.Length != 6) {
            throw new ArgumentException(
                $"{content} should be of length 6 after stripping leading characters and alpha channel, but is {content.Length}",
                content);
        }

        return Color.FromArgb(alpha, content[4..].ParseHexString()[0],
            content[2..4].ParseHexString()[0], content[..2].ParseHexString()[0]);
    }

    public static string ToAss(this bool b) => b ? "-1" : "0";

    public static bool ParseBool(string content) => content != "0";
}
