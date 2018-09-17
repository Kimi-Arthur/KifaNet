using System.Collections.Generic;
using System.Linq;

namespace Pimix.Subtitle.Ass {
    public abstract class AssSection {
        public abstract string SectionTitle { get; }

        public virtual IEnumerable<AssLine> AssLines => new List<AssLine>();

        public override string ToString()
            => $"[{SectionTitle}]\r\n{string.Join("\r\n", AssLines.Select(line => line.ToString()))}\r\n";
    }
}
