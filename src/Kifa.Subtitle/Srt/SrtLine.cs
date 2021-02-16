using System;
using Kifa.Subtitle.Ass;
using NLog;

namespace Kifa.Subtitle.Srt {
    public class SrtLine {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public int Index { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public SrtTextElement Text { get; set; }

        public static SrtLine Parse(string s) {
            logger.Trace($"Parsing srt line: {s}");
            var lines = s.Trim('\n')
                .Split('\n', 3, StringSplitOptions.RemoveEmptyEntries);
            var times = lines[1].Replace(',', '.').Split(new[] {
                " --> "
            }, StringSplitOptions.None);
            return new SrtLine {
                Index = int.Parse(lines[0]),
                StartTime = TimeSpan.Parse(times[0]),
                EndTime = TimeSpan.Parse(times[1]),
                Text = new SrtTextElement {
                    Content = lines.Length == 3 ? lines[2] : ""
                }
            };
        }

        public AssDialogue ToAss()
            => new AssDialogue {
                Layer = 2,
                Text = Text.ToAss(),
                Start = StartTime,
                End = EndTime,
                Style = AssStyle.SubtitleStyle
            };

        public override string ToString()
            => $"{Index}\n" +
               $"{StartTime:hh\\:mm\\:ss\\,fff} --> {EndTime:hh\\:mm\\:ss\\,fff}\n" +
               $"{string.Join("", Text)}";
    }
}
