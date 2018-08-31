using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public class AssStylesSection : AssSection
    {
        public override string SectionTitle => "V4+ Styles";

        public List<string> Format
            => new List<string>
            {
                "Name",
                "Fontname",
                "Fontsize",
                "PrimaryColour",
                "SecondaryColour",
                "OutlineColour",
                "BackColour",
                "Bold",
                "Italic",
                "Underline",
                "StrikeOut",
                "ScaleX",
                "ScaleY",
                "Spacing",
                "Angle",
                "BorderStyle",
                "Outline",
                "Shadow",
                "Alignment",
                "MarginL",
                "MarginR",
                "MarginV",
                "Encoding"
            };

        public List<AssStyle> Styles { get; set; }

        public override IEnumerable<AssLine> AssLines
        {
            get
            {
                yield return new AssLine("Format", Format);
                foreach (var style in Styles)
                {
                    yield return style;
                }
            }
        }
    }
}
