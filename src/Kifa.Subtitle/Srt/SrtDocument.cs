using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kifa.Subtitle.Srt;

public class SrtDocument {
    public List<SrtLine> Lines { get; set; } = [];

    public static SrtDocument Parse(string s) {
        var normalized = s.Replace("\r\n", "\n").Trim();
        if (string.IsNullOrWhiteSpace(normalized)) {
            return new SrtDocument();
        }

        var blocks = Regex.Split(normalized, @"\n{2,}");
        return new SrtDocument {
            Lines = blocks.Select(SrtLine.Parse).ToList()
        };
    }

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
