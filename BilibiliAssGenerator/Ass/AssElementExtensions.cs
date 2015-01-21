using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public static class AssElementExtensions
    {
        public static string GenerateAssText(this bool b)
            => b ? "-1" : "0";

        public static string GenerateAssText(this Color color)
            => "&H\{color.A : X2}\{color.B : X2}\{color.G : X2}\{color.R : X2}";

        public static string GenerateAssText(this double f)
            => "\{f : f2}";

        public static string GenerateAssText(this Enum f)
            => "\{f : d}";

        public static string GenerateAssText(this int d)
            => d.ToString();

        public static string GenerateAssText(this string str)
            => str;

        public static string GenerateAssText(this TimeSpan t)
            => "\{t : "h\\:mm\\:ss\\.ff"}";
    }

}
