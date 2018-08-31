using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public abstract class AssSection : AssElement
    {
        public abstract string SectionTitle { get; }

        public virtual IEnumerable<AssLine> AssLines
            => new List<AssLine>();

        public override string GenerateAssText()
            => $"[{SectionTitle}]\r\n{(string.Join("\r\n", AssLines.Select(line => line.GenerateAssText())))}\r\n";
    }
}
