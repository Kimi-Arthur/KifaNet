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

        List<Tuple<string, string, long>> raw = new List<Tuple<string, string, long>>
        {
            new Tuple<string, string, long>("encrypted.bin", "8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF", 13659),
            new Tuple<string, string, long>("data-encrypted.bin", "0A43E6858977A39B861420FA31877030A0E683F1E25FFCF6A42098E6CB4C4948", 65536)
        };

        List<Tuple<string, string, long>> encrypted = new List<Tuple<string, string, long>>
        {
            new Tuple<string, string, long>("raw.bin", "E1223699AFBDFBB5252D7CCEA23A40BFCE8DD4834A53E52A75C778BEC3C72706", 13664),
            new Tuple<string, string, long>("data.bin", "2222C7B3D3D1896636DDC0642F8CC2F882D23CECCE1D2FEE7678B5B3587A3163", 65552)
        };

        [TestMethod]
        public void PimixCryptoStreamDecryptionReadBasicTest()
        {
            foreach (var item in raw)
            {
                ICryptoTransform transform = aesAlgorithm.CreateDecryptor();

                using (var stream = new PimixCryptoStream(File.OpenRead(item.Item1), transform, item.Item3, true))
                {
                    var info = FileInformation.GetInformation(stream, FileProperties.SHA256 | FileProperties.Size | FileProperties.SliceMD5);
                    Assert.AreEqual(item.Item3, info.Size);
                    Assert.AreEqual(item.Item2, info.SHA256);
                    Assert.AreEqual(item.Item2, info.SHA256);

                    foreach (var b in new List<int> { 8, 11, 12, 16, 33, 100 })
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        var output = new MemoryStream();
                        stream.CopyTo(output, b);

                        info = FileInformation.GetInformation(output, FileProperties.SHA256 | FileProperties.Size | FileProperties.SliceMD5);
                        Assert.AreEqual(item.Item3, info.Size);
                        Assert.AreEqual(item.Item2, info.SHA256);
                        Assert.AreEqual(item.Item2, info.SHA256);
                    }

                }
            }
        }

        [TestMethod]
        public void PimixCryptoStreamDecryptionReadIncompleteEndTest()
        {
            foreach (var item in raw)
            {
                ICryptoTransform transform = aesAlgorithm.CreateDecryptor();

                using (var stream = new PimixCryptoStream(File.OpenRead(item.Item1), transform, item.Item3, true))
                {
                    var baseStream = new MemoryStream();
                    stream.CopyTo(baseStream);

                    foreach (var b in new List<int> { 8, 11, 12, 16, 33, 100 })
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
        }

        [TestMethod]
        public void PimixCryptoStreamEncryptionReadBasicTest()
        {
            foreach (var item in encrypted)
            {
                ICryptoTransform transform = aesAlgorithm.CreateEncryptor();

                using (var stream = new PimixCryptoStream(File.OpenRead(item.Item1), transform, item.Item3, false))
                {
                    var info = FileInformation.GetInformation(stream, FileProperties.SHA256 | FileProperties.Size | FileProperties.SliceMD5);
                    Assert.AreEqual(item.Item3, info.Size);
                    Assert.AreEqual(item.Item2, info.SHA256);
                    Assert.AreEqual(item.Item2, info.SHA256);

                    foreach (var b in new List<int> { 8, 11, 12, 16, 33, 100 })
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        var output = new MemoryStream();
                        stream.CopyTo(output, b);

                        info = FileInformation.GetInformation(output, FileProperties.SHA256 | FileProperties.Size | FileProperties.SliceMD5);
                        Assert.AreEqual(item.Item3, info.Size);
                        Assert.AreEqual(item.Item2, info.SHA256);
                        Assert.AreEqual(item.Item2, info.SHA256);
                    }
                }
            }
        }
    }
}