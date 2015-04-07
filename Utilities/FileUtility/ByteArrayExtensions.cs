using System;

namespace Pimix.Storage
{
    static class ByteArrayExtensions
    {
        public static string Dump(this byte[] input)
            => BitConverter.ToString(input).Replace("-", "");
    }
}
