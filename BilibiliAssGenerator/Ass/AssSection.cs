using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public abstract class AssSection : AssElement
    {
        public virtual string SectionTitle { get; } = "Section Title";
        public virtual IEnumerable<AssLine> AssLines { get; } = new List<AssLine>();
        public override string GenerateText()
            => "[\{SectionTitle}]\n\{string.Join("\n", AssLines.Select(line => line.GenerateText()))}";
    }
}
