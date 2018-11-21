using System.Collections.Generic;
using System.Linq;

namespace Pimix.Subtitle.Ass {
    public abstract class AssSection {
        public abstract string SectionTitle { get; }

        public virtual IEnumerable<AssLine> AssLines => new List<AssLine>();

        public override string ToString()
            => $"{SectionTitle}\r\n{string.Join("\r\n", AssLines.Select(line => line.ToString()))}\r\n";

        public static AssSection Parse(string title, IEnumerable<string> lines) {
            switch (title) {
                case AssScriptInfoSection.SectionHeader:
                    return AssScriptInfoSection.Parse(lines);
                case AssStylesSection.SectionHeader:
                    return AssStylesSection.Parse(lines);
                case AssEventsSection.SectionHeader:
                    return new AssEventsSection();
                default:
                    return null;
            }
        }
    }
}
