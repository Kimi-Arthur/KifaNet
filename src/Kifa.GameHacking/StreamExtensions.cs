using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Kifa.GameHacking;

public static class StreamExtensions {
    public static void Reset(this Stream stream) {
        stream.Seek(0, SeekOrigin.Begin);
    }

    public static void AssertNumbers<T>(this Stream stream, params T[] numbers)
        where T : IBinaryInteger<T> {
        stream.AssertNumbers(false, numbers);
    }

    public static void AssertNumbers<T>(this Stream stream, bool bigEndian, params T[] numbers)
        where T : IBinaryInteger<T> {
        foreach (var expected in numbers) {
            var actual = stream.GetNumber<T>(bigEndian);

            if (expected != actual) {
                throw new DecodeException($"Expected to read {expected}, but got '{actual}'.");
            }
        }
    }

    public static T GetNumber<T>(this Stream stream, bool bigEndian = false)
        where T : IBinaryInteger<T> {
        var zero = T.Zero;
        var bytes = new byte[zero.GetByteCount()];
        var isUnsigned = typeof(T).GetInterface("IUnsignedNumber`1") != null;
        stream.ReadExactly(bytes);
        return bigEndian
            ? T.ReadBigEndian(bytes, isUnsigned)
            : T.ReadLittleEndian(bytes, isUnsigned);
    }

    public static void AssertStrings(this Stream stream, params string[] strings) {
        stream.AssertStrings(null, strings);
    }

    public static void AssertStrings(this Stream stream, Encoding? encoding,
        params string[] strings) {
        foreach (var expected in strings) {
            var actual = stream.GetString(encoding?.GetBytes(expected).Length ?? expected.Length,
                encoding: encoding);
            if (expected != actual) {
                throw new DecodeException($"Expected to read '{expected}', but got '{actual}'.");
            }
        }
    }

    public static void AssertNullEndedStrings(this Stream stream, params string[] strings) {
        stream.AssertNullEndedStrings(null, strings);
    }

    public static void AssertNullEndedStrings(this Stream stream, Encoding? encoding = null,
        params string[] strings) {
        foreach (var expected in strings) {
            var actual = stream.GetNullEndedString(encoding: encoding);
            if (expected != actual) {
                throw new DecodeException($"Expected to read '{expected}', but got '{actual}'.");
            }
        }
    }

    public static string GetString(this Stream stream, int length, Encoding? encoding = null) {
        encoding ??= Encoding.Latin1;
        return encoding.GetString(stream.GetBytes(length));
    }

    public static string GetNullEndedString(this Stream stream, Encoding? encoding = null) {
        encoding ??= Encoding.Latin1;

        var bytes = new List<byte>();
        for (var b = stream.ReadByte(); b > 0; b = stream.ReadByte()) {
            bytes.Add((byte) b);
        }

        return encoding.GetString(bytes.ToArray());
    }

    public static byte[] GetBytes(this Stream stream, int length) {
        var buffer = new byte[length];
        stream.ReadExactly(buffer, 0, length);
        return buffer;
    }
}
