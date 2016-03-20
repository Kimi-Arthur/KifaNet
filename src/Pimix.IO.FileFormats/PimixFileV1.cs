using System;
using System.IO;
using System.Security.Cryptography;
using Pimix.Cryptography;

namespace Pimix.IO.FileFormats
{
    /// <summary>
    /// V1 file format.
    /// 
    /// Common header for v1 and onward:
    ///     B0~3: 0x0123 0x1225
    ///     B4~5: Version Number
    ///     B6~7: Header Length (hl)
    ///     B8~(hl-1): Other parts
    /// 
    /// V1 header:
    ///     B0~3: 0x0123 0x1225
    ///     B4~7: 0x0001 0x0030
    ///     B8~15: File Length (int64)
    ///     B16~47: SHA256 (256bit)
    /// </summary>
    public class PimixFileV1 : PimixFile
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
                    encodedStream.Seek(16, SeekOrigin.Begin);
                    byte[] sha256Bytes = new byte[32];
                    encodedStream.Read(sha256Bytes, 0, 32);
                    Info.Id = ":" + sha256Bytes.ToHexString();
                }

                Info = FileInformation.Get(Info.Id);
            }

            if (Info.Size == null)
            {
                // If we only miss the file size, we can get from the header.
                byte[] sizeBytes = new byte[8];
                encodedStream.Seek(8, SeekOrigin.Begin);
                encodedStream.Read(sizeBytes, 0, 8);
                Info.Size = sizeBytes.ToInt64();
            }

            ICryptoTransform decoder;
            using (Aes aesAlgorithm = new AesCryptoServiceProvider())
            {
                aesAlgorithm.Padding = PaddingMode.ANSIX923;
                aesAlgorithm.Key = Info.EncryptionKey.ParseHexString();
                aesAlgorithm.Mode = CipherMode.ECB;
                decoder = aesAlgorithm.CreateDecryptor();
            }

            return new PimixCryptoStream(new PatchedStream(encodedStream) { IgnoreBefore = 0x30 }, decoder, Info.Size.Value, true);
        }

        public override Stream GetEncodeStream(Stream rawStream)
        {
            Info = Info ?? new FileInformation();

            Info.AddProperties(rawStream, FileProperties.Size | FileProperties.SHA256);

            if (Info.EncryptionKey == null)
                throw new ArgumentException("Encryption key must be given before calling");

            byte[] header = new byte[48];
            new byte[] { 0x01, 0x23, 0x12, 0x25, 0x00, 0x01, 0x00, 0x30 }.CopyTo(header, 0);
            Info.Size.Value.ToByteArray().CopyTo(header, 8);
            Info.SHA256.ParseHexString().CopyTo(header, 16);

            ICryptoTransform encoder;
            using (Aes aesAlgorithm = new AesCryptoServiceProvider())
            {
                aesAlgorithm.Padding = PaddingMode.ANSIX923;
                aesAlgorithm.Key = Info.EncryptionKey.ParseHexString();
                aesAlgorithm.Mode = CipherMode.ECB;
                encoder = aesAlgorithm.CreateEncryptor();
            }

            return new PatchedStream(new PimixCryptoStream(rawStream, encoder, Info.Size.Value.RoundDown(16) + 16, false)) { BufferBefore = header };
        }
    }
}
