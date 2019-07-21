using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pimix.Subtitle.Srt {
    public class SrtDocument {
        static readonly Regex linePattern = new Regex(@"\d+([^\n]*\n){2}([^\n]+\n)*(\n|$)");

        public List<SrtLine> Lines { get; set; }

        public static SrtDocument Parse(string s)
            => new SrtDocument {
                Lines = linePattern.Matches(s)
                    .Select(m => SrtLine.Parse(m.Value)).ToList()
            };

        public void Sort() {
            Lines.Sort((lineA, lineB) => lineA.StartTime.CompareTo(lineB.StartTime));
        }

        public void Renumber() {
            for (var i = 0; i < Lines.Count; i++) {
                Lines[i].Index = i + 1;
            }
        }

        public override string ToString() => string.Join("\n\n", Lines) + "\n\n";
    }
}
