using System.Drawing;

namespace Pimix.Subtitle.Ass {
    public static class AssFormatter {
        public static string ToAss(this Color color)
            => $"&H{255 - color.A:X2}{color.B:X2}{color.G:X2}{color.R:X2}";

        public static Color ParseColor(string content)
            => Color.FromArgb(255 - content.Substring(2, 2).ParseHexString()[0],
                content.Substring(4, 2).ParseHexString()[0],
                content.Substring(6, 2).ParseHexString()[0],
                content.Substring(8, 2).ParseHexString()[0]
            );

        public static string ToAss(this bool b) => b ? "-1" : "0";

        public static bool ParseBool(string content) => content != "0";
    }
}
