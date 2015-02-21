using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public class AssEventsSection : AssSection
    {
        public override string SectionTitle { get; } = "Events";

        public List<string> Format
            => new List<string>
            {
                "Layer",
                "Start",
                "End",
                "Style",
                "Actor",
                "MarginL",
                "MarginR",
                "MarginV",
                "Effect",
                "Text"
            };

        public List<AssEvent> Events { get; set; }

        public override IEnumerable<AssLine> AssLines
        {
            get
            {
                yield return new AssLine("Format", Format);
            }
        }
    }
}
