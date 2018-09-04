using System.Collections.Generic;
using System.Linq;

namespace Pimix.Ass {
    public class AssDocument : AssElement {
        public List<AssSection> Sections { get; set; } = new List<AssSection>();

        public override string GenerateAssText()
            => string.Join("\r\n", Sections.Select(s => s.GenerateAssText()));
    }
}
