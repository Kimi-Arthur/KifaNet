using System.Collections.Generic;
using System.Linq;

namespace Pimix.Subtitle.Ass {
    public class AssDocument {
        public List<AssSection> Sections { get; set; } = new List<AssSection>();

        public override string ToString()
            => string.Join("\r\n", Sections.Select(s => s.ToString()));
    }
}
