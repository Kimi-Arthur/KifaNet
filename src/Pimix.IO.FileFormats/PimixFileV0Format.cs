using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Pimix.Cryptography;

namespace Pimix.IO.FileFormats
{
    /// <summary>
    /// Legacy file format with encryption and information header.
    ///
    /// There is a verbose header part, 0x1225 bytes long and starts with "0x01231225".
    /// Information wise, it only contains SHA256 and file size.
    /// SHA256 starts at 0x0e90 (3728) and is in hex string format (thus occupies 64 bytes)
    /// File size starts at 0x073e (1854) and will end with space.
    ///
    /// We only provide decoder for this format.
    ///
    /// </summary>
    public class PimixFileV0Format : PimixFileFormat
    {
        public static PimixFileFormat Get(string fileSpec)
        {
            var specs = fileSpec.Split(new char[] { ';' });
            foreach (var spec in specs)
            {
                if (spec == "v0")
                {
                    return new PimixFileV0Format();
                }
            }

            return null;
        }

        public override string ToString()
            => "v0";

        public override Stream GetDecodeStream(Stream encodedStream, FileInformation info)
        {
            if (info.EncryptionKey == null)
            {
                // We have to get the data from server...

                if (info.Id == null)
                {
                    // We need to get the secondary id from the stream as ":SHA256".
                    encodedStream.Seek(3728, SeekOrigin.Begin);
                    byte[] sha256Bytes = new byte[64];
                    encodedStream.Read(sha256Bytes, 0, 64);
                    info.Id = ":" + Encoding.UTF8.GetString(sha256Bytes, 0, 64);
                }

                info = FileInformation.Get(info.Id);
            }

            if (info.Size == null)
            {
                // If we only miss the file size, we can get from the header.
                encodedStream.Seek(1854, SeekOrigin.Begin);
                byte[] sizeBytes = new byte[92];
                info.Size = long.Parse(Encoding.UTF8.GetString(sizeBytes, 0, 92));
            }

            ICryptoTransform decoder;
            using (Aes aesAlgorithm = new AesCryptoServiceProvider())
            {
                aesAlgorithm.Padding = PaddingMode.ANSIX923;
                aesAlgorithm.Key = info.EncryptionKey.ParseHexString();
                aesAlgorithm.Mode = CipherMode.ECB;
                decoder = aesAlgorithm.CreateDecryptor();
            }

            return new PimixCryptoStream(new PatchedStream(encodedStream) { IgnoreBefore = 0x1225 }, decoder, info.Size.Value, true);
        }

        public override Stream GetEncodeStream(Stream rawStream, FileInformation info)
        {
            throw new NotImplementedException();
        }
    }
}
