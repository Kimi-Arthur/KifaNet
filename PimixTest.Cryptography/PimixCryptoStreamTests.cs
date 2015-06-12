using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix;
using Pimix.Cryptography;
using Pimix.Storage;

namespace PimixTest.Cryptography
{
    [TestClass]
    public class PimixCryptoStreamTests
    {
        Aes aesAlgorithm = new AesCryptoServiceProvider
        {
            Padding = PaddingMode.ANSIX923,
            Key = "C7C37D56DD70FD6258BDD01AED083C88432EC27536DF9328D6329382183DB795".ParseHexString(),
            Mode = CipherMode.ECB
        };

        string rawSHA256 = "8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF";
        long rawSize = 13659;

        string encryptedSHA256 = "E1223699AFBDFBB5252D7CCEA23A40BFCE8DD4834A53E52A75C778BEC3C72706";
        long encryptedSize = 13664;

        [TestMethod]
        public void PimixCryptoStreamDecryptionReadBasicTest()
        {
            ICryptoTransform transform = aesAlgorithm.CreateDecryptor();

            using (var stream = new PimixCryptoStream(File.OpenRead("encrypted.bin"), transform, rawSize, true))
            {
                var info = FileInformation.GetInformation(stream, FileProperties.SHA256 | FileProperties.Size | FileProperties.SliceMD5);
                Assert.AreEqual(rawSize, info.Size);
                Assert.AreEqual(rawSHA256, info.SHA256);
                Assert.AreEqual(rawSHA256, info.SHA256);

                foreach (var b in new List<int> { 8, 10, 11, 12, 19, 33, 100 })
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    var output = new MemoryStream();
                    stream.CopyTo(output, b);

                    info = FileInformation.GetInformation(output, FileProperties.SHA256 | FileProperties.Size | FileProperties.SliceMD5);
                    Assert.AreEqual(rawSize, info.Size);
                    Assert.AreEqual(rawSHA256, info.SHA256);
                    Assert.AreEqual(rawSHA256, info.SHA256);
                }

            }
        }

        [TestMethod]
        public void PimixCryptoStreamDecryptionReadIncompleteEndTest()
        {
            ICryptoTransform transform = aesAlgorithm.CreateDecryptor();

            using (var stream = new PimixCryptoStream(File.OpenRead("encrypted.bin"), transform, rawSize, true))
            {
                var baseStream = new MemoryStream();
                stream.CopyTo(baseStream);

                foreach (var b in new List<int> { 8, 10, 11, 12, 19, 33, 100 })
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.Seek(-b, SeekOrigin.End);
                    baseStream.Seek(-b, SeekOrigin.End);
                    for (int i = 0; i < b; i++)
                    {
                        Assert.AreEqual(baseStream.ReadByte(), stream.ReadByte());
                    }
                }
            }
        }

        [TestMethod]
        public void PimixCryptoStreamEncryptionReadBasicTest()
        {
            ICryptoTransform transform = aesAlgorithm.CreateEncryptor();

            using (var stream = new PimixCryptoStream(File.OpenRead("raw.bin"), transform, encryptedSize, false))
            {
                var info = FileInformation.GetInformation(stream, FileProperties.SHA256 | FileProperties.Size | FileProperties.SliceMD5);
                Assert.AreEqual(encryptedSize, info.Size);
                Assert.AreEqual(encryptedSHA256, info.SHA256);
                Assert.AreEqual(encryptedSHA256, info.SHA256);

                foreach (var b in new List<int> { 8, 10, 11, 12, 19, 33, 100 })
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    var output = new MemoryStream();
                    stream.CopyTo(output, b);

                    info = FileInformation.GetInformation(output, FileProperties.SHA256 | FileProperties.Size | FileProperties.SliceMD5);
                    Assert.AreEqual(encryptedSize, info.Size);
                    Assert.AreEqual(encryptedSHA256, info.SHA256);
                    Assert.AreEqual(encryptedSHA256, info.SHA256);
                }
            }
        }
    }
}