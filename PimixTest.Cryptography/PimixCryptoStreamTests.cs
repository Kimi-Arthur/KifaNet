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
                aesAlgorithm.Key = "C7C37D56DD70FD6258BDD01AED083C88432EC27536DF9328D6329382183DB795".ParseHexString();
                aesAlgorithm.Mode = CipherMode.ECB;

                transform = aesAlgorithm.CreateDecryptor();
            }

            using (var stream = new PimixCryptoStream(File.OpenRead("encrypted.bin"), transform, 13659, 0x1225))
            {
                var info = FileInformation.GetInformation(stream, FileProperties.SHA256 | FileProperties.Size);
                Assert.AreEqual(13659, info.Size);
                Assert.AreEqual("8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF", info.SHA256);
            }
        }
    }
}
