using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.IO;
using Pimix.IO.FileFormats;

namespace PimixTest.IO.FileFormats
{
    [TestClass]
    public class PimixFileV1Tests
    {
        [TestMethod]
        public void RandomDataTest()
        {
            byte[] data = new byte[(64 << 20) + 100];
            new Random().NextBytes(data);
            FileInformation fs = new FileInformation { EncryptionKey = "C7C37D56DD70FD6258BDD01AED083C88432EC27536DF9328D6329382183DB795" };

            var rangeList = new List<Tuple<int, int>>
            {
                new Tuple<int, int>(0, (64 << 20) + 100),
                new Tuple<int, int>(0, 64 << 20),
                new Tuple<int, int>(0, (64 << 20) - 16),
                new Tuple<int, int>(0, (64 << 20) + 16),
                new Tuple<int, int>(0, (64 << 20) - 8)
            };
            foreach (var item in rangeList)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    using (MemoryStream encrypted = new MemoryStream())
                    {
                        using (Stream encryptionStream = new PimixFileV1 { Info = fs }.GetEncodeStream(ms))
                        {
                            encryptionStream.CopyTo(encrypted);
                            using (Stream output = new PimixFileV1 { Info = fs }.GetDecodeStream(encrypted))
                            {
                                ms.Position = 0;
                                var fs1 = FileInformation.GetInformation(ms, FileProperties.Size | FileProperties.SHA256);
                                var fs2 = FileInformation.GetInformation(output, FileProperties.Size | FileProperties.SHA256);
                                Assert.AreEqual(fs1.Size, fs2.Size);
                                Assert.AreEqual(fs1.SHA256, fs2.SHA256);
                            }
                        }
                    }
                }
            }
        }
    }
}
