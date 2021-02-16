using System.Collections.Generic;
using System.Linq;

namespace Kifa.Subtitle.Ass {
    public abstract class AssSection {
        public abstract string SectionTitle { get; }

        public virtual IEnumerable<AssLine> AssLines => new List<AssLine>();

        public override string ToString()
            => $"{SectionTitle}\n{string.Join("\n", AssLines.Select(line => line.ToString()))}\n";

        public static AssSection Parse(AssStylesSection stylesSection, string title, IEnumerable<string> lines) {
            switch (title) {
                case AssScriptInfoSection.SectionHeader:
                    return AssScriptInfoSection.Parse(lines);
                case AssStylesSection.SectionHeader:
                    return AssStylesSection.Parse(lines);
                case AssEventsSection.SectionHeader:
                    return AssEventsSection.Parse(stylesSection, lines);
                default:
                    return null;
            }
        }
    }
}
