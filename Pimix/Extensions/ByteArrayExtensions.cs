using System;

namespace Pimix
{
    public static class ByteArrayExtensions
    {
        public static string ToHexString(this byte[] input)
            => BitConverter.ToString(input).Replace("-", "");
    }
}
