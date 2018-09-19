using System;
using System.Collections.Generic;
using System.Linq;
using Pimix.Subtitle.Ass;

namespace Pimix.Subtitle.Srt {
    public class SrtLine {
        public int Index { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public List<SrtTextElement> Text { get; set; }

        public static SrtLine Parse(string s) {
            var lines = s.Trim()
                .Split(new[] {"\r\n", "\n"}, 3, StringSplitOptions.RemoveEmptyEntries);
            var times = lines[1].Replace(',', '.').Split(new[] {" --> "}, StringSplitOptions.None);
            return new SrtLine {
                Index = int.Parse(lines[0]),
                StartTime = TimeSpan.Parse(times[0]),
                EndTime = TimeSpan.Parse(times[1]),
                Text = new List<SrtTextElement> {
                    new SrtTextElement {Content = lines[2]}
                }
            };
        }

        public AssDialogue ToAss()
            => new AssDialogue {
                Layer = 1,
                Text = new AssDialogueText {
                    TextElements = Text.Select(x => x.ToAss()).ToList()
                },
                Start = StartTime,
                End = EndTime,
                Style = AssStyle.SubtitleStyle
            };

        public override string ToString()
            => $"{Index}\r\n" +
               $"{StartTime:hh\\:mm\\:ss\\,fff} --> {EndTime:hh\\:mm\\:ss\\,fff}\r\n" +
               $"{string.Join("", Text)}";
    }
}
