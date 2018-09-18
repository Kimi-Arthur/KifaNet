using System.Drawing;

namespace Pimix.Subtitle.Ass {
    public static class AssFormatter {
        public static string ToString(Color color)
            => $"&H{255 - color.A:X2}{color.B:X2}{color.G:X2}{color.R:X2}";
    }
}
