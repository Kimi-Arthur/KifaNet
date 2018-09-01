using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Pimix {
    public static class StringExtensions {
        public static string Format(this string format, Dictionary<string, string> parameters) {
            if (format == null) {
                throw new ArgumentNullException(nameof(format));
            }

            var result = format;

            if (parameters == null) {
                return result;
            }

            foreach (var p in parameters) {
                result = result.Replace("{" + p.Key + "}", p.Value);
            }

            return result;
        }

        public static string Format(this string format, params object[] args) {
            if (format == null) {
                throw new ArgumentNullException(nameof(format));
            }

            return string.Format(format, args);
        }

        static Dictionary<string, long> SymbolMap;

        public static long ParseSizeString(this string data) {
            if (string.IsNullOrEmpty(data)) {
                throw new ArgumentNullException(nameof(data));
            }

            if (SymbolMap == null) {
                SymbolMap = new Dictionary<string, long>();

                var lastValue = SymbolMap[""] = 1;
                foreach (var item in "KMGTPEZY") {
                    SymbolMap[item.ToString()] = lastValue = lastValue << 10;
                }
            }

            var match = new Regex(@"^(\d+)([^B])B?$").Match(data.ToUpper());

            return long.Parse(match.Groups[1].Value) *
                   SymbolMap.GetValueOrDefault(match.Groups[2].Value, 0);
        }

        public static byte[] ParseHexString(this string hexString) {
            if (hexString == null || hexString.Length % 2 == 1) {
                throw new ArgumentException("Not hex string");
            }

            var hexBytes = new byte[hexString.Length / 2];
            for (var i = 0; i < hexString.Length; i += 2) {
                hexBytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return hexBytes;
        }

        public static TimeSpan ParseTimeSpanString(this string timeSpanString) {
            if (string.IsNullOrEmpty(timeSpanString)) {
                return TimeSpan.Zero;
            }

            if (timeSpanString.EndsWith("hr")) {
                return TimeSpan.FromHours(
                    double.Parse(timeSpanString.Substring(0, timeSpanString.Length - 2)));
            }

            if (timeSpanString.EndsWith("min")) {
                return TimeSpan.FromMinutes(
                    double.Parse(timeSpanString.Substring(0, timeSpanString.Length - 3)));
            }

            if (timeSpanString.EndsWith("s")) {
                return TimeSpan.FromSeconds(
                    double.Parse(timeSpanString.Substring(0, timeSpanString.Length - 1)));
            }

            return TimeSpan.FromSeconds(double.Parse(timeSpanString));
        }
    }
}
