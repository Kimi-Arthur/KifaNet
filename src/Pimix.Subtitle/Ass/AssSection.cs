using System;
using System.Collections.Generic;
using System.Linq;

namespace Pimix.Subtitle.Ass {
    public abstract class AssSection {
        public abstract string SectionTitle { get; }

        public virtual IEnumerable<AssLine> AssLines => new List<AssLine>();

        public override string ToString()
            => $"[{SectionTitle}]\r\n{string.Join("\r\n", AssLines.Select(line => line.ToString()))}\r\n";

        public static AssSection Parse(string content) {
            var lines = content.Split(new[] {"\r\n"}, StringSplitOptions.None);
            var title = lines[0].Substring(1, lines[0].Length - 2);
            switch (title) {
                case "Script Info":
                    return new AssScriptInfoSection();
                case "V4+ Styles":
                    return new AssStylesSection();
                case "Events":
                    return new AssEventsSection();
                default:
                    return null;
            }
        }
    }
}
