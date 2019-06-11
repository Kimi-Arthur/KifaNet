using System;
using System.Collections.Generic;
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
    ///     B48~55: Shard start offset (int64)
    ///     B56~63: Shard length (int64)
    /// </summary>
    public class PimixFileV2Format : PimixFileFormat {
        const byte HeaderLength = 0x40;

        public static long ShardSize { get; set; } = 1L << 30;

        static readonly PimixFileV2Format Instance = new PimixFileV2Format();

        public static PimixFileFormat Get(string fileUri) {
            if (fileUri.EndsWith(".v2")) {
                return Instance;
            }

            return null;
        }

        public override string ToString() => "v2";

        public override Stream GetDecodeStream(List<Stream> encodedStreams, string encryptionKey = null) {
            var firstStream = encodedStreams.First();

            firstStream.Seek(16, SeekOrigin.Begin);
            var sha256Bytes = new byte[32];
            firstStream.Read(sha256Bytes, 0, 32);

            if (encryptionKey == null) {
                // We need to get the secondary id from the stream as ":SHA256".
                var id = ":" + sha256Bytes.ToHexString();

                encryptionKey = FileInformation.Client.Get(id).EncryptionKey;
            }

            var sizeBytes = new byte[8];
            firstStream.Seek(8, SeekOrigin.Begin);
            firstStream.Read(sizeBytes, 0, 8);
            var length = sizeBytes.ToInt64();


            ICryptoTransform encoder;
            using (Aes aesAlgorithm = new AesCryptoServiceProvider()) {
                aesAlgorithm.Padding = PaddingMode.None;
                aesAlgorithm.Key = encryptionKey.ParseHexString();
                aesAlgorithm.Mode = CipherMode.ECB;
                encoder = aesAlgorithm.CreateEncryptor();
            }

            var counter = GetCounter(sha256Bytes);
            var streams = new List<Stream>();
            foreach (var encodedStream in encodedStreams) {
                streams.Add(new PatchedStream(encodedStream) {IgnoreBefore = 64});
            }

            return new CounterCryptoStream(new MultiReadStream(streams), encoder, length, counter);
        }

        public override List<Stream> GetEncodeStreams(Stream rawStream, FileInformation info) {
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
            var streams = new List<Stream>();
            for (long position = 0; position < length; position += ShardSize) {
                var shardLength = Math.Min(ShardSize, length - position);
                position.ToByteArray().CopyTo(header, 48);
                shardLength.ToByteArray().CopyTo(header, 56);
                streams.Add(new PatchedStream(new CounterCryptoStream(new PatchedStream(rawStream) {
                        IgnoreBefore = position
                    }, encoder,
                    shardLength, counter.ToArray())) {
                    BufferBefore = header.ToArray()
                });

                counter.Add(ShardSize / encoder.InputBlockSize);
            }

            return streams;
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
