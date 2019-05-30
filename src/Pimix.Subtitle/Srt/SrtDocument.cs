using System;
using System.Collections.Generic;
using System.Linq;

namespace Pimix.Subtitle.Srt {
    public class SrtDocument {
        public List<SrtLine> Lines { get; set; }

        public static SrtDocument Parse(string s)
            => new SrtDocument {
                Lines = s.Trim().Split(new[] {
                        "\n\n", "\n\n"
                    }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(SrtLine.Parse).ToList()
            };

        public void Sort() {
            Lines.Sort((lineA, lineB) => lineA.StartTime.CompareTo(lineB.StartTime));
        }

        public void Renumber() {
            for (var i = 0; i < Lines.Count; i++) {
                Lines[i].Index = i + 1;
            }
        }

        public override string ToString() => string.Join("\n\n", Lines);
    }
}
