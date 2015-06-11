using System;
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

        [TestMethod]
        public void PimixCryptoStreamReadBasicTest()
        {
            ICryptoTransform transform = aesAlgorithm.CreateDecryptor();

            using (var stream = new PimixCryptoStream(File.OpenRead("encrypted.bin"), transform, 13659, 0x1225))
            {
                var info = FileInformation.GetInformation(stream, FileProperties.SHA256 | FileProperties.Size | FileProperties.SliceMD5);
                Assert.AreEqual(13659, info.Size);
                Assert.AreEqual("8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF", info.SHA256);
                Assert.AreEqual("8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF", info.SHA256);

                stream.Seek(0, SeekOrigin.Begin);
                var st1 = new MemoryStream();
                stream.CopyTo(st1, 16);

                info = FileInformation.GetInformation(st1, FileProperties.SHA256 | FileProperties.Size | FileProperties.SliceMD5);
                Assert.AreEqual(13659, info.Size);
                Assert.AreEqual("8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF", info.SHA256);
                Assert.AreEqual("8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF", info.SHA256);

                stream.Seek(0, SeekOrigin.Begin);
                var st2 = new MemoryStream();
                stream.CopyTo(st2, 11);

                st1.Seek(0, SeekOrigin.Begin);
                st2.Seek(0, SeekOrigin.Begin);
                for (int i = 0; i < 13659; ++i)
                    if (st1.ReadByte() != st2.ReadByte())
                    {
                        Console.WriteLine(i);
                    }
                info = FileInformation.GetInformation(st2, FileProperties.SHA256 | FileProperties.Size | FileProperties.SliceMD5);
                Assert.AreEqual(13659, info.Size);
                Assert.AreEqual("8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF", info.SHA256);
                Assert.AreEqual("8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF", info.SHA256);

                stream.Seek(0, SeekOrigin.Begin);
                var st3 = new MemoryStream();
                stream.CopyTo(st3, 19);

                info = FileInformation.GetInformation(st3, FileProperties.SHA256 | FileProperties.Size | FileProperties.SliceMD5);
                Assert.AreEqual(13659, info.Size);
                Assert.AreEqual("8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF", info.SHA256);
                Assert.AreEqual("8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF", info.SHA256);
            }
        }
    }
}