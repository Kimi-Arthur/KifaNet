using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Kifa.Cryptography;

namespace Kifa.IO.FileFormats;

/// <summary>
///     V2 file format. V2 uses counter based encryption instead of plain.
///     Common header for v1 and onward:
///     B0~3: 0x0123 0x1225
///     B4~5: Version Number
///     B6~7: Header Length (hl)
///     B8~(hl-1): Other parts
///     V2 header (almost the same as v1 except version):
///     B0~3: 0x0123 0x1225
///     B4~7: 0x0002 0x0030
///     B8~15: File Length (int64)
///     B16~47: SHA256 (256bit)
/// </summary>
public class KifaFileV2Format : KifaFileFormat {
    public static readonly KifaFileV2Format Instance = new();
    const byte HeaderLength = 0x30;

    public static KifaFileFormat? Get(string fileUri) => fileUri.EndsWith(".v2") ? Instance : null;

    public override string ToString() => "v2";

    public override Stream GetDecodeStream(Stream encodedStream, string? encryptionKey = null) {
        var sha256Bytes = new byte[32];
        encodedStream.Seek(16, SeekOrigin.Begin);
        encodedStream.Read(sha256Bytes, 0, 32);

        if (encryptionKey == null) {
            // We need to get the secondary id from the stream as ":SHA256".
            var id = ":" + sha256Bytes.ToHexString();

            encryptionKey = FileInformation.Client.Get(id).EncryptionKey;
        }

        var sizeBytes = new byte[8];
        encodedStream.Seek(8, SeekOrigin.Begin);
        encodedStream.Read(sizeBytes, 0, 8);
        var size = sizeBytes.ToInt64();

        ICryptoTransform encoder;
        using (Aes aesAlgorithm = new AesCryptoServiceProvider()) {
            aesAlgorithm.Padding = PaddingMode.None;
            aesAlgorithm.Key = encryptionKey.ParseHexString();
            aesAlgorithm.Mode = CipherMode.ECB;
            encoder = aesAlgorithm.CreateEncryptor();
        }

        var counter = GetCounter(sha256Bytes);
        return new CounterCryptoStream(new PatchedStream(encodedStream) {
            IgnoreBefore = HeaderLength
        }, encoder, size, counter);
    }

    public override long HeaderSize => 0x30;

    public override Stream GetEncodeStream(Stream rawStream, FileInformation info) {
        info.AddProperties(rawStream, FileProperties.Size | FileProperties.Sha256);

        if (info.EncryptionKey == null) {
            throw new ArgumentException("Encryption key must be given before calling");
        }

        if (info.Size == null) {
            throw new ArgumentException("File length cannot be got.");
        }

        var length = info.Size.Value;
        var header = new byte[HeaderLength];
        new byte[] { 0x01, 0x23, 0x12, 0x25, 0x00, 0x02, 0x00, HeaderLength }.CopyTo(header, 0);
        length.ToByteArray().CopyTo(header, 8);
        var sha256 = info.Sha256.ParseHexString();
        sha256.CopyTo(header, 16);

        ICryptoTransform encoder;
        using (Aes aesAlgorithm = new AesCryptoServiceProvider()) {
            aesAlgorithm.Padding = PaddingMode.None;
            aesAlgorithm.Key = info.EncryptionKey.ParseHexString();
            aesAlgorithm.Mode = CipherMode.ECB;
            encoder = aesAlgorithm.CreateEncryptor();
        }

        var counter = GetCounter(sha256);
        return new PatchedStream(new CounterCryptoStream(rawStream, encoder, length, counter)) {
            BufferBefore = header.ToArray()
        };
    }

    static byte[] GetCounter(byte[] infoSha256) {
        var result = new byte[16];
        for (var i = 0; i < 16; i++) {
            result[i] = (byte) (infoSha256[i] ^ infoSha256[i + 16]);
        }

        return result;
    }
}
