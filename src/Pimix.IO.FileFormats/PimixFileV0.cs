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
    public class PimixFileV0 : PimixFile
    {
        public override Stream GetDecodeStream(Stream encodedStream)
        {
            Info = Info ?? new FileInformation();
            if (Info.EncryptionKey == null)
            {
                // We have to get the data from server...

                if (Info.Id == null)
                {
                    // We need to get the secondary id from the stream as ":SHA256".
                    encodedStream.Seek(3728, SeekOrigin.Begin);
                    byte[] sha256Bytes = new byte[64];
                    encodedStream.Read(sha256Bytes, 0, 64);
                    Info.Id = ":" + Encoding.UTF8.GetString(sha256Bytes, 0, 64);
                }

                Info = FileInformation.Get(Info.Id);
            }

            if (Info.Size == null)
            {
                // If we only miss the file size, we can get from the header.
                encodedStream.Seek(1854, SeekOrigin.Begin);
                byte[] sizeBytes = new byte[92];
                Info.Size = long.Parse(Encoding.UTF8.GetString(sizeBytes, 0, 92));
            }

            ICryptoTransform decoder;
            using (Aes aesAlgorithm = new AesCryptoServiceProvider())
            {
                aesAlgorithm.Padding = PaddingMode.ANSIX923;
                aesAlgorithm.Key = Info.EncryptionKey.ParseHexString();
                aesAlgorithm.Mode = CipherMode.ECB;
                decoder = aesAlgorithm.CreateDecryptor();
            }

            return new PimixCryptoStream(new PatchedStream(encodedStream) { IgnoreBefore = 0x1225 }, decoder, Info.Size.Value, true);
        }

        public override Stream GetEncodeStream(Stream rawStream)
        {
            throw new NotImplementedException();
        }
    }
}
