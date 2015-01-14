using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public class AssStyle
    {
        public string Name { get; set; }
        public string Fontname { get; set; }
        public int Fontsize { get; set; }
        public Color PrimaryColour { get; set; }
        public Color SecondaryColour { get; set; }
        public Color OutlineColour { get; set; }
        public Color BackColour { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Strikeout { get; set; }
        public int ScaleX { get; set; }
        public int ScaleY { get; set; }
        public int Spacing { get; set; }
        public double Angle { get; set; }

    }
}
