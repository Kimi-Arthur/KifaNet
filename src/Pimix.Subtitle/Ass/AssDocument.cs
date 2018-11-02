using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pimix.Subtitle.Ass {
    public class AssDocument {
        public List<AssSection> Sections { get; set; } = new List<AssSection>();

        public override string ToString()
            => string.Join("\r\n", Sections.Select(s => s.ToString()));

        public static AssDocument Parse(Stream stream) {
            using (var sr = new StreamReader(stream)) {
                return Parse(sr.ReadToEnd());
            }
        }

        static AssDocument Parse(string content) {
            return new AssDocument {
                Sections = content.Split("\r\n\r\n").Select(AssSection.Parse).ToList()
            };
        }
    }
}
