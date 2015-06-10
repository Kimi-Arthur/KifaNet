using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pimix
{
    public static class MathExtensions
    {
        public static long RoundUp(this long value, long period)
            => value % period == 0 ? value : value - value % period + period;

        public static int RoundUp(this int value, int period)
            => value % period == 0 ? value : value - value % period + period;

        public static long RoundDown(this long value, long period)
            => value % period == 0 ? value : value - value % period;

        public static int RoundDown(this int value, int period)
            => value % period == 0 ? value : value - value % period;
    }
}
