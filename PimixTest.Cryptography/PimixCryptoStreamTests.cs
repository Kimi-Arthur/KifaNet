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
        [TestMethod]
        public void PimixCryptoStreamReadTest()
        {
            ICryptoTransform transform;
            using (Aes aesAlgorithm = new AesCryptoServiceProvider())
            {
                aesAlgorithm.Padding = PaddingMode.ANSIX923;
                aesAlgorithm.Key = "18D4ED9941F0F7832E56AFA8C229BB3CC6ADEFAB191B8B7037616238424C4E66".ParseHexString();
                aesAlgorithm.Mode = CipherMode.ECB;

                transform = aesAlgorithm.CreateDecryptor();
            }

            using (var stream = new PimixCryptoStream(File.OpenRead("C:/Users/Kimi/Downloads/g.wmv"), transform, 900914919, 0x1225))
            {
                using (Stream fs = File.OpenWrite("C:/Users/Kimi/Downloads/g_.wmv"))
                {
                    stream.CopyTo(fs, 32 << 20);
                }

                //var info = FileInformation.GetInformation(stream, FileProperties.SHA256 | FileProperties.Size);
                //Assert.AreEqual(13659, info.Size);
                //Assert.AreEqual("8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF", info.SHA256);
            }
        }
    }
}
