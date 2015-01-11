using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public class AssStylesSection : AssSection
    {
        public override string SectionTitle { get; } = "V4+ Styles";
        public List<string> Format { get; set; }
        public List<AssStyle> Styles { get; set; }
        public override IEnumerable<AssLine> AssLines
        {
            get
            {
                yield return new AssLine("Format", Format);
            }
        }
    }
}
