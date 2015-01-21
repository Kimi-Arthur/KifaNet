using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public class AssStyle : AssLine
    {
        public enum BorderStyleType
        {
            OutlineWithDropShadow = 1,
            OpaqueBox = 3
        }

        public static string DefaultFontname = "Simhei";

        public static AssStyle DefaultStyle = null;

        public string Name { get; set; }

        public string Fontname { get; set; } = DefaultFontname;

        public int Fontsize { get; set; }

        public Color PrimaryColour { get; set; }

        public Color SecondaryColour { get; set; }

        public Color OutlineColour { get; set; }

        public Color BackColour { get; set; }

        public bool Bold { get; set; }

        public bool Italic { get; set; }

        public bool Underline { get; set; }

        public bool StrikeOut { get; set; }

        public int ScaleX { get; set; }

        public int ScaleY { get; set; }

        public int Spacing { get; set; }

        public double Angle { get; set; }

        public BorderStyleType BorderStyle { get; set; }

        int outline;
        public int Outline
        {
            get
            {
                return outline;
            }
            set
            {
                if (value < 0 || value > 4)
                    throw new ArgumentOutOfRangeException(nameof(Outline));
                outline = value;
            }
        }

        int shadow;
        public int Shadow
        {
            get
            {
                return shadow;
            }
            set
            {
                if (value < 0 || value > 4)
                    throw new ArgumentOutOfRangeException(nameof(Shadow));
                shadow = value;
            }
        }

        public AssAlignment Alignment { get; set; }

        public int MarginL { get; set; }

        public int MarginR { get; set; }

        public int MarginV { get; set; }

        public int Encoding { get; set; }

        public override string Key => "Style";

        public override IEnumerable<string> Values
            => new List<string>
            {
                Name.GenerateAssText(),
                Fontname.GenerateAssText(),
                Fontsize.GenerateAssText(),
                PrimaryColour.GenerateAssText(),
                SecondaryColour.GenerateAssText(),
                OutlineColour.GenerateAssText(),
                BackColour.GenerateAssText(),
                Bold.GenerateAssText(),
                Italic.GenerateAssText(),
                Underline.GenerateAssText(),
                StrikeOut.GenerateAssText(),
                ScaleX.GenerateAssText(),
                ScaleY.GenerateAssText(),
                Spacing.GenerateAssText(),
                Angle.GenerateAssText(),
                BorderStyle.GenerateAssText(),
                Outline.GenerateAssText(),
                Shadow.GenerateAssText(),
                Alignment.GenerateAssText(),
                MarginL.GenerateAssText(),
                MarginR.GenerateAssText(),
                MarginV.GenerateAssText(),
                Encoding.GenerateAssText()
            };

        public string ValidName
            => Name == "Default" ? "*Default" : Name;
    }
}
