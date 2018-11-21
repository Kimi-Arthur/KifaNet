using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pimix.Subtitle.Ass {
    public class AssDocument {
        static readonly Regex sectionHeaderPattern = new Regex(@"^\[.*\]$");
        
        public List<AssSection> Sections { get; set; } = new List<AssSection>();

        public override string ToString()
            => string.Join("\r\n", Sections.Select(s => s.ToString()));

        public static AssDocument Parse(Stream stream) {
            using (var sr = new StreamReader(stream)) {
                return Parse(sr.ReadToEnd());
            }
        }

        static AssDocument Parse(string content) {
            var document = new AssDocument();

            var lines = content.Split("\r\n");
            var startLine = -1;
            for (int i = 0; i < lines.Length; i++) {
                if (sectionHeaderPattern.Match(lines[i]).Success) {
                    if (startLine >= 0) {
                        document.Sections.Add(AssSection.Parse(lines[startLine], lines.Take(i).Skip(startLine + 1)));
                    }

                    startLine = i;
                }
            }

            return document;
        }
    }
}
