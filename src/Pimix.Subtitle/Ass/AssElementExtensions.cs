using System;
using System.Drawing;

namespace Pimix.Subtitle.Ass {
    public static class AssElementExtensions {
        public static string ToString(this bool b) => b ? "-1" : "0";

        public static string ToString(this Color color)
            => $"&H{color.A:X2}{color.B:X2}{color.G:X2}{color.R:X2}";

        public static string ToString(this double f) => $"{f:f2}";

        public static string ToString(this Enum f) => $"{f:d}";

        public static string ToString(this int d) => d.ToString();

        public static string ToString(this string str) => str;

        public static string ToString(this TimeSpan t) => $"{t:h\\:mm\\:ss\\.ff}";
    }
}
