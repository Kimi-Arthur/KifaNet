using System;

namespace Pimix.Utilities
{
    static class ByteArrayExtensions
    {
        public static string Dump(this byte[] input)
            => BitConverter.ToString(input).Replace("-", "");
    }
}
