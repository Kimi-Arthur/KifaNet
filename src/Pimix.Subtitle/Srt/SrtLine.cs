using System;

namespace Pimix.Subtitle.Srt {
    public class SrtLine {
        public int Index { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Text { get; set; }
    }
}
