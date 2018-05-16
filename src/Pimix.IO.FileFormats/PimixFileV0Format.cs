using System.IO;
using System.Security.Cryptography;
using System.Text;
using Pimix.Cryptography;

namespace Pimix.IO.FileFormats {
    /// <summary>
    ///     Legacy file format with encryption and information header.
    ///     There is a verbose header part, 0x1225 bytes long and starts with "0x01231225".
    ///     Information wise, it only contains SHA256 and file size.
    ///     SHA256 starts at 0x0e90 (3728) and is in hex string format (thus occupies 64 bytes)
    ///     File size starts at 0x073e (1854) and will end with space.
    ///     We only provide decoder for this format.
    /// </summary>
    public class PimixFileV0Format : PimixFileFormat {
        public static PimixFileFormat Get(string fileSpec) {
            var specs = fileSpec.Split(';');
            foreach (var spec in specs)
                if (spec == "v0")
                    return new PimixFileV0Format();

            return null;
        }

        public override string ToString() => "v0";

        public override Stream GetDecodeStream(Stream encodedStream, string encryptionKey = null) {
            if (encryptionKey == null) {
                // We need to get the secondary id from the stream as ":SHA256".
                encodedStream.Seek(3728, SeekOrigin.Begin);
                var sha256Bytes = new byte[64];
                encodedStream.Read(sha256Bytes, 0, 64);
                var id = ":" + Encoding.UTF8.GetString(sha256Bytes, 0, 64);

                encryptionKey = FileInformation.Get(id).EncryptionKey;
            }

            encodedStream.Seek(1854, SeekOrigin.Begin);
            var sizeBytes = new byte[92];
            encodedStream.Read(sizeBytes, 0, 92);
            var size = long.Parse(Encoding.UTF8.GetString(sizeBytes, 0, 92));

            ICryptoTransform decoder;
            using (Aes aesAlgorithm = new AesCryptoServiceProvider()) {
                aesAlgorithm.Padding = PaddingMode.ANSIX923;
                aesAlgorithm.Key = encryptionKey.ParseHexString();
                aesAlgorithm.Mode = CipherMode.ECB;
                decoder = aesAlgorithm.CreateDecryptor();
            }

            return new PimixCryptoStream(new PatchedStream(encodedStream) {IgnoreBefore = 0x1225},
                decoder, size, true);
        }
    }
}
