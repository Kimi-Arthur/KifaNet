using System.Drawing;

namespace Pimix.Subtitle.Srt {
    public class SrtTextElement {
        public string Content { get; set; }

        public bool? Bold { get; set; }

        public bool? Italic { get; set; }

        public bool? Underline { get; set; }

        public Color? FontColor { get; set; }

        public override string ToString() {
            var s = Content;
            if (Bold == true) {
                s = $"<b>{s}</b>";
            }

            if (Italic == true) {
                s = $"<i>{s}</i>";
            }

            if (Underline == true) {
                s = $"<u>{s}</u>";
            }

            if (FontColor.HasValue) {
                s = $"<font color=\"{FontColor.Value.R:X2}" +
                    $"{FontColor.Value.G:X2}" +
                    $"{FontColor.Value.B:X2}\">{s}</font>";
            }

            return s;
        }
    }
}
