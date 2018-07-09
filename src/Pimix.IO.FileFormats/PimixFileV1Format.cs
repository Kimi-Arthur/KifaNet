using System;
using System.IO;
using System.Security.Cryptography;
using Pimix.Cryptography;

namespace Pimix.IO.FileFormats {
    /// <summary>
    ///     V1 file format.
    ///     Common header for v1 and onward:
    ///     B0~3: 0x0123 0x1225
    ///     B4~5: Version Number
    ///     B6~7: Header Length (hl)
    ///     B8~(hl-1): Other parts
    ///     V1 header:
    ///     B0~3: 0x0123 0x1225
    ///     B4~7: 0x0001 0x0030
    ///     B8~15: File Length (int64)
    ///     B16~47: SHA256 (256bit)
    /// </summary>
    public class PimixFileV1Format : PimixFileFormat {
        public static PimixFileFormat Get(string fileUri) {
            if (fileUri.EndsWith(".v1")) {
                return new PimixFileV1Format();
            }

            return null;
        }

        public override string ToString() => "v1";

        public override Stream GetDecodeStream(Stream encodedStream, string encryptionKey = null) {
            if (encryptionKey == null) {
                // We need to get the secondary id from the stream as ":SHA256".
                encodedStream.Seek(16, SeekOrigin.Begin);
                var sha256Bytes = new byte[32];
                encodedStream.Read(sha256Bytes, 0, 32);
                var id = ":" + sha256Bytes.ToHexString();

                encryptionKey = FileInformation.Get(id).EncryptionKey;
            }

            var sizeBytes = new byte[8];
            encodedStream.Seek(8, SeekOrigin.Begin);
            encodedStream.Read(sizeBytes, 0, 8);
            var size = sizeBytes.ToInt64();

            ICryptoTransform decoder;
            using (Aes aesAlgorithm = new AesCryptoServiceProvider()) {
                aesAlgorithm.Padding = PaddingMode.ANSIX923;
                aesAlgorithm.Key = encryptionKey.ParseHexString();
                aesAlgorithm.Mode = CipherMode.ECB;
                decoder = aesAlgorithm.CreateDecryptor();
            }

            return new PimixCryptoStream(new PatchedStream(encodedStream) {IgnoreBefore = 0x30},
                decoder, size, true);
        }

        public override Stream GetEncodeStream(Stream rawStream, FileInformation info) {
            info.AddProperties(rawStream, FileProperties.Size | FileProperties.SHA256);

            if (info.EncryptionKey == null)
                throw new ArgumentException("Encryption key must be given before calling");

            var header = new byte[48];
            new byte[] {0x01, 0x23, 0x12, 0x25, 0x00, 0x01, 0x00, 0x30}.CopyTo(header, 0);
            info.Size.Value.ToByteArray().CopyTo(header, 8);
            info.SHA256.ParseHexString().CopyTo(header, 16);

            ICryptoTransform encoder;
            using (Aes aesAlgorithm = new AesCryptoServiceProvider()) {
                aesAlgorithm.Padding = PaddingMode.ANSIX923;
                aesAlgorithm.Key = info.EncryptionKey.ParseHexString();
                aesAlgorithm.Mode = CipherMode.ECB;
                encoder = aesAlgorithm.CreateEncryptor();
            }

            return new PatchedStream(new PimixCryptoStream(rawStream, encoder,
                info.Size.Value.RoundDown(16) + 16, false)) {BufferBefore = header};
        }
    }
}
