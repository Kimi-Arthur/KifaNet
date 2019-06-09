using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix;
using Pimix.Cryptography;
using Pimix.IO;

namespace PimixTest.Cryptography {
    [TestClass]
    public class CounterCryptoStreamTests {
        readonly Aes aesAlgorithm = new AesCryptoServiceProvider {
            Padding = PaddingMode.None,
            Key = "C7C37D56DD70FD6258BDD01AED083C88432EC27536DF9328D6329382183DB795"
                .ParseHexString(),
            Mode = CipherMode.ECB
        };

        readonly byte[] initialCounter = "E1223699AFBDFBB5252D7CCEA23A40BF".ParseHexString();

        readonly List<Tuple<string, string, string, long>> encrypted =
            new List<Tuple<string, string, string, long>> {
                new Tuple<string, string, string, long>("data.bin",
                    "0A43E6858977A39B861420FA31877030A0E683F1E25FFCF6A42098E6CB4C4948",
                    "BB1DC1539DE7930FCD11D720A471C58EDCB074B54372209F199CEC12C56B83EB", 65536)
            };

        [TestMethod]
        public void CounterCryptoStreamRoundTripTest() {
            foreach (var item in encrypted) {
                var transform = aesAlgorithm.CreateEncryptor();

                var baseStream = File.OpenRead(item.Item1);
                var baseInfo = FileInformation.GetInformation(baseStream,
                    FileProperties.Sha256 | FileProperties.Size);
                using (var stream = new CounterCryptoStream(baseStream, transform,
                    item.Item4, initialCounter)) {
                    using (var roundTripStream =
                        new CounterCryptoStream(stream, transform, item.Item4, initialCounter)) {
                        var roundTripInfo = FileInformation.GetInformation(roundTripStream,
                            FileProperties.Sha256 | FileProperties.Size);

                        Assert.AreEqual(baseInfo.Size, roundTripInfo.Size);
                        Assert.AreEqual(baseInfo.Sha256, roundTripInfo.Sha256);
                    }
                }
            }
        }

        [TestMethod]
        public void CounterCryptoStreamEncryptionReadBasicTest() {
            foreach (var item in encrypted) {
                var transform = aesAlgorithm.CreateEncryptor();

                using (var stream = new CounterCryptoStream(File.OpenRead(item.Item1), transform,
                    item.Item4, initialCounter)) {
                    var info = FileInformation.GetInformation(stream,
                        FileProperties.Sha256 | FileProperties.Size | FileProperties.SliceMd5);
                    Assert.AreEqual(item.Item4, info.Size);
                    Assert.AreEqual(item.Item3, info.Sha256);

                    foreach (var b in new List<int> {
                        8,
                        11,
                        12,
                        16,
                        33,
                        100
                    }) {
                        stream.Seek(0, SeekOrigin.Begin);
                        var output = new MemoryStream();
                        stream.CopyTo(output, b);

                        info = FileInformation.GetInformation(output,
                            FileProperties.Sha256 | FileProperties.Size | FileProperties.SliceMd5);
                        Assert.AreEqual(item.Item4, info.Size);
                        Assert.AreEqual(item.Item3, info.Sha256);
                    }
                }
            }
        }

        [TestMethod]
        public void CounterCryptoStreamDecryptionReadIncompleteEndTest() {
            foreach (var item in encrypted) {
                var transform = aesAlgorithm.CreateEncryptor();

                using (var stream = new CounterCryptoStream(File.OpenRead(item.Item1), transform,
                    item.Item4, initialCounter)) {
                    var baseStream = new MemoryStream();
                    stream.CopyTo(baseStream);

                    foreach (var b in new List<int> {
                        8,
                        11,
                        12,
                        16,
                        33,
                        100
                    }) {
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.Seek(-b, SeekOrigin.End);
                        baseStream.Seek(-b, SeekOrigin.End);
                        for (var i = 0; i < b; i++) {
                            Assert.AreEqual(baseStream.ReadByte(), stream.ReadByte());
                        }
                    }
                }
            }
        }
    }
}
