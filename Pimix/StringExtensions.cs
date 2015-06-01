using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pimix
{
    public static class StringExtensions
    {
        public static string Format(this string format, Dictionary<string, string> parameters)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            string result = format;
            foreach (var p in parameters)
            {
                result = result.Replace("{" + p.Key + "}", p.Value);
            }

            return result;
        }

        public static string Format(this string format, params Object[] args)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            return string.Format(format, args);
        }
    }
}
