using System;

namespace Pimix {
    public static class ByteArrayExtensions {
        public static string ToHexString(this byte[] input)
            => BitConverter.ToString(input).Replace("-", "");

        public static byte[] ToByteArray(this long input) {
            var result = new byte[8];
            for (var i = 0; i < result.Length; i++) {
                result[i] = (byte) input;
                input >>= 8;
            }

            return result;
        }

        public static long ToInt64(this byte[] input) {
            long result = 0;
            for (var i = input.Length - 1; i >= 0; i--) {
                result = (result << 8) + input[i];
            }

            return result;
        }

        public static void Add(this byte[] data, long addition) {
            for (int i = data.Length - 1; i >= 0; i--) {
                addition += data[i];
                data[i] = (byte) addition;
                addition >>= 8;
                if (addition == 0) {
                    return;
                }
            }
        }

        public static string ToBase64(this byte[] data) => Convert.ToBase64String(data);
    }
}
