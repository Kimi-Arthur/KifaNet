using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Pimix;
using Pimix.Cryptography;
using Pimix.IO;
using Xunit;

namespace PimixTest.Cryptography {
    public class CounterCryptoStreamTests {
        readonly Aes aesAlgorithm = new AesCryptoServiceProvider {
            Padding = PaddingMode.None,
            Key = "C7C37D56DD70FD6258BDD01AED083C88432EC27536DF9328D6329382183DB795"
                .ParseHexString(),
            Mode = CipherMode.ECB
        };

        readonly byte[] initialCounter = "E1223699AFBDFBB5252D7CCEA23A40BF".ParseHexString();

        readonly
            List<(string rawFile, long rawSize, string rawHash, string encryptedFile, long encryptedSize, string
                encryptedHash)> data =
                new List<(string rawFile, long rawSize, string rawHash, string encryptedFile, long encryptedSize, string
                    encryptedHash)> {
                    ("data-1.raw.bin", 65536, "0A43E6858977A39B861420FA31877030A0E683F1E25FFCF6A42098E6CB4C4948",
                        "data-1.ctr.bin", 65536,
                        "BB1DC1539DE7930FCD11D720A471C58EDCB074B54372209F199CEC12C56B83EB"),
                    ("data-2.raw.bin", 13659, "8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF",
                        "data-2.ctr.bin", 13659,
                        "787EE39879AB57FBEA5A9999C67FBF7FC1FB0AD0E2E3100AF8C3AD115014BF0C")
                };

        [Fact]
        public void CounterCryptoStreamRoundTripTest() {
            foreach (var (rawFile, rawSize, rawHash, encryptedFile, encryptedSize, encryptedHash) in data) {
                var transform = aesAlgorithm.CreateEncryptor();

                var baseStream = File.OpenRead(rawFile);
                var baseInfo = FileInformation.GetInformation(baseStream,
                    FileProperties.Sha256 | FileProperties.Size);
                using (var stream = new CounterCryptoStream(baseStream, transform,
                    encryptedSize, initialCounter)) {
                    using (var roundTripStream =
                        new CounterCryptoStream(stream, transform, rawSize, initialCounter)) {
                        var roundTripInfo = FileInformation.GetInformation(roundTripStream,
                            FileProperties.Sha256 | FileProperties.Size);

                        Assert.Equal(baseInfo.Size, roundTripInfo.Size);
                        Assert.Equal(baseInfo.Sha256, roundTripInfo.Sha256);
                    }
                }
            }
        }

        [Fact]
        public void CounterCryptoStreamDecryptionReadBasicTest() {
            foreach (var (rawFile, rawSize, rawHash, encryptedFile, encryptedSize, encryptedHash) in data) {
                var transform = aesAlgorithm.CreateEncryptor();

                using (var stream = new CounterCryptoStream(File.OpenRead(encryptedFile), transform,
                    rawSize, initialCounter)) {
                    var info = FileInformation.GetInformation(stream,
                        FileProperties.Sha256 | FileProperties.Size | FileProperties.SliceMd5);
                    Assert.Equal(rawSize, info.Size);
                    Assert.Equal(rawHash, info.Sha256);

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
                        Assert.Equal(rawSize, info.Size);
                        Assert.Equal(rawHash, info.Sha256);
                    }
                }
            }
        }

        [Fact]
        public void CounterCryptoStreamDecryptionReadIncompleteEndTest() {
            foreach (var (rawFile, rawSize, rawHash, encryptedFile, encryptedSize, encryptedHash) in data) {
                var transform = aesAlgorithm.CreateEncryptor();

                using (var stream = new CounterCryptoStream(File.OpenRead(encryptedFile), transform,
                    rawSize, initialCounter)) {
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
                            Assert.Equal(baseStream.ReadByte(), stream.ReadByte());
                        }
                    }
                }
            }
        }

        [Fact]
        public void CounterCryptoStreamEncryptionReadBasicTest() {
            foreach (var (rawFile, rawSize, rawHash, encryptedFile, encryptedSize, encryptedHash) in data) {
                var transform = aesAlgorithm.CreateEncryptor();

                using (var stream = new CounterCryptoStream(File.OpenRead(rawFile), transform,
                    encryptedSize, initialCounter)) {
                    var info = FileInformation.GetInformation(stream,
                        FileProperties.Sha256 | FileProperties.Size | FileProperties.SliceMd5);
                    Assert.Equal(encryptedSize, info.Size);
                    Assert.Equal(encryptedHash, info.Sha256);

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
                        Assert.Equal(encryptedSize, info.Size);
                        Assert.Equal(encryptedHash, info.Sha256);
                    }
                }
            }
        }
    }
}
