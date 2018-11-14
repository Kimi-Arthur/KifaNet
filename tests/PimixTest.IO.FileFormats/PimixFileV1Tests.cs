using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.IO;
using Pimix.IO.FileFormats;

namespace PimixTest.IO.FileFormats {
    [TestClass]
    public class PimixFileV1Tests {
        [TestMethod]
        public void RandomDataTest() {
            var EncryptionKey = "C7C37D56DD70FD6258BDD01AED083C88432EC27536DF9328D6329382183DB795";

            var data = new byte[(64 << 20) + 100];
            new Random().NextBytes(data);

            var rangeList = new List<Tuple<int, int>> {
                new Tuple<int, int>(0, 100),
                new Tuple<int, int>(0, (64 << 20) + 100),
                new Tuple<int, int>(0, 64 << 20),
                new Tuple<int, int>(0, (64 << 20) - 16),
                new Tuple<int, int>(0, (64 << 20) + 16),
                new Tuple<int, int>(0, (64 << 20) - 8)
            };

            foreach (var item in rangeList) {
                using (var ms = new MemoryStream(data, item.Item1, item.Item2))
                using (var encrypted = new MemoryStream())
                using (var encryptionStream = new PimixFileV1Format().GetEncodeStream(ms,
                    new FileInformation {
                        EncryptionKey = EncryptionKey,
                        Size = item.Item2 - item.Item1
                    })) {
                    encryptionStream.CopyTo(encrypted);
                    using (var output =
                        new PimixFileV1Format().GetDecodeStream(encrypted, EncryptionKey)) {
                        var fs1 = FileInformation.GetInformation(ms,
                            FileProperties.Size | FileProperties.SHA256);
                        var fs2 = FileInformation.GetInformation(output,
                            FileProperties.Size | FileProperties.SHA256);
                        Assert.AreEqual(fs1.Size, fs2.Size);
                        Assert.AreEqual(fs1.Sha256, fs2.Sha256);
                    }
                }
            }
        }
    }
}
