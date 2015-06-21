using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
    /// </summary>
    public class PimixFileV0 : PimixFile
    {
        public override Stream GetDecodeStream(Stream encodedStream, FileInformation fileInformation = null)
        {
            fileInformation = fileInformation ?? new FileInformation();
            if (fileInformation.EncryptionKey == null)
            {
                // We have to get the data from server...

                if (fileInformation.Id == null)
                {
                    // We need to get the secondary id from the stream as ":SHA256".
                    encodedStream.Seek(3728, SeekOrigin.Begin);
                    byte[] sha256Bytes = new byte[64];
                    encodedStream.Read(sha256Bytes, 0, 64);
                    fileInformation.Id = ":" + Encoding.UTF8.GetString(sha256Bytes, 0, 64);
                }

                fileInformation = FileInformation.Get(fileInformation.Id);
            }

            if (fileInformation.Size == null)
            {
                // If we only miss the file size, we can get from the header.
                encodedStream.Seek(1854, SeekOrigin.Begin);
                byte[] sizeBytes = new byte[92];
                fileInformation.Size = long.Parse(Encoding.UTF8.GetString(sizeBytes, 0, 92));
            }

            ICryptoTransform decoder;
            using (Aes aesAlgorithm = new AesCryptoServiceProvider())
            {
                aesAlgorithm.Padding = PaddingMode.ANSIX923;
                aesAlgorithm.Key = fileInformation.EncryptionKey.ParseHexString();
                aesAlgorithm.Mode = CipherMode.ECB;
                decoder = aesAlgorithm.CreateDecryptor();
            }

            return new PimixCryptoStream(new PartialStream(encodedStream, 0x1225), decoder, fileInformation.Size.Value, true);
        }

        public override Stream GetEncodeStream(Stream rawStream, FileInformation fileInformation = null)
        {
            throw new NotImplementedException();
        }
    }
}
