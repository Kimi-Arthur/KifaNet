using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kifa.Subtitle.Ass;

public class AssDocument {
    static readonly Regex sectionHeaderPattern = new(@"^\[.*\]$");
    static readonly Regex separator = new("(\r)?\n");

    public List<AssSection> Sections { get; set; } = new();

    public override string ToString() => string.Join("\n", Sections.Select(s => s.ToString()));

    public static AssDocument Parse(Stream stream) {
        using var sr = new StreamReader(stream);
        return Parse(sr.ReadToEnd());
    }

    public static AssDocument Parse(string content) {
        var document = new AssDocument();

        AssStylesSection stylesSection = null;
        var lines = separator.Split(content);
        var startLine = -1;
        for (var i = 0; i < lines.Length; i++) {
            if (sectionHeaderPattern.Match(lines[i]).Success) {
                if (startLine >= 0) {
                    var section = AssSection.Parse(stylesSection, lines[startLine],
                        lines.Take(i).Skip(startLine + 1));
                    if (section != null) {
                        document.Sections.Add(section);
                        if (section is AssStylesSection assStylesSection) {
                            stylesSection = assStylesSection;
                        }
                    }
                }

                startLine = i;
            }
        }

        if (startLine >= 0) {
            // No need to check whether this is a styles section as this is the last one.
            var section = AssSection.Parse(stylesSection, lines[startLine],
                lines.Skip(startLine + 1));
            if (section != null) {
                document.Sections.Add(section);
            }
        }

        return document;
    }
}
