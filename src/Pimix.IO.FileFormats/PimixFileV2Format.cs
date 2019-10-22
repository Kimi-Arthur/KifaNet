using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Pimix.Cryptography;

namespace Pimix.IO.FileFormats {
    /// <summary>
    ///     V2 file format.
    ///     Common header for v1 and onward:
    ///     B0~3: 0x0123 0x1225
    ///     B4~5: Version Number
    ///     B6~7: Header Length (hl)
    ///     B8~(hl-1): Other parts
    ///     V2 header:
    ///     B0~3: 0x0123 0x1225
    ///     B4~7: 0x0002 0x0040
    ///     B8~15: File Length (int64)
    ///     B16~47: SHA256 (256bit)
    ///     B48~55: Shard start offset (int64) (including this byte)
    ///     B56~63: Shard end offset (int64) (excluding this byte)
    /// </summary>
    public class PimixFileV2Format : PimixFileFormat {
        const byte HeaderLength = 0x40;

        public static PimixFileFormat Get(string fileUri) {
            if (fileUri.EndsWith(".v2")) {
                var shardString = fileUri.Substring(0, fileUri.Length - 3).Split('.').Last();
                var shardSegments = shardString.Split('-');
                return new PimixFileV2Format
                    {ShardStart = long.Parse(shardSegments[0]), ShardEnd = long.Parse(shardSegments[1])};
            }

            return null;
        }

        public long ShardStart { get; set; }

        public long ShardEnd { get; set; }

        public override string ToString() => "v2";

        public override Stream GetDecodeStream(Stream encodedStream, string encryptionKey = null) {
            encodedStream.Seek(16, SeekOrigin.Begin);
            var sha256Bytes = new byte[32];
            encodedStream.Read(sha256Bytes, 0, 32);

            if (encryptionKey == null) {
                // We need to get the secondary id from the stream as ":SHA256".
                var id = ":" + sha256Bytes.ToHexString();

                encryptionKey = FileInformation.Client.Get(id).EncryptionKey;
            }

            var shardStartBytes = new byte[8];
            encodedStream.Seek(48, SeekOrigin.Begin);
            encodedStream.Read(shardStartBytes, 0, 8);
            var shardStart = shardStartBytes.ToInt64();
            var shardEndBytes = new byte[8];
            encodedStream.Seek(56, SeekOrigin.Begin);
            encodedStream.Read(shardEndBytes, 0, 8);
            var shardLength = shardEndBytes.ToInt64() - shardStartBytes.ToInt64();

            ICryptoTransform encoder;
            using (Aes aesAlgorithm = new AesCryptoServiceProvider()) {
                aesAlgorithm.Padding = PaddingMode.None;
                aesAlgorithm.Key = encryptionKey.ParseHexString();
                aesAlgorithm.Mode = CipherMode.ECB;
                encoder = aesAlgorithm.CreateEncryptor();
            }

            var counter = GetCounter(sha256Bytes);
            counter.Add(shardStart / encoder.InputBlockSize);

            return new CounterCryptoStream(new PatchedStream(encodedStream) {IgnoreBefore = 64}, encoder, shardLength,
                counter);
        }

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
            new byte[] {
                0x01, 0x23, 0x12, 0x25, 0x00, 0x02, 0x00, HeaderLength
            }.CopyTo(header, 0);
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
            counter.Add(ShardStart / encoder.InputBlockSize);
            ShardStart.ToByteArray().CopyTo(header, 48);
            ShardEnd.ToByteArray().CopyTo(header, 56);
            return new PatchedStream(new CounterCryptoStream(new PatchedStream(rawStream) {
                    IgnoreBefore = ShardStart
                }, encoder,
                ShardEnd - ShardStart, counter)) {
                BufferBefore = header.ToArray()
            };
        }

        static byte[] GetCounter(byte[] infoSha256) {
            var result = new byte[16];
            for (int i = 0; i < 16; i++) {
                result[i] = (byte) (infoSha256[i] ^ infoSha256[i + 16]);
            }

            return result;
        }
    }
}
