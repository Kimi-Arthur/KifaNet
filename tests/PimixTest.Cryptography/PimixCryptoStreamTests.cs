using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Pimix;
using Pimix.Cryptography;
using Kifa.IO;
using Xunit;

namespace PimixTest.Cryptography {
    public class PimixCryptoStreamTests {
        readonly Aes aesAlgorithm = new AesCryptoServiceProvider {
            Padding = PaddingMode.ANSIX923,
            Key = "C7C37D56DD70FD6258BDD01AED083C88432EC27536DF9328D6329382183DB795"
                .ParseHexString(),
            Mode = CipherMode.ECB
        };

        readonly
            List<(string rawFile, long rawSize, string rawHash, string encryptedFile, long encryptedSize, string
                encryptedHash)> data =
                new List<(string rawFile, long rawSize, string rawHash, string encryptedFile, long encryptedSize, string
                    encryptedHash)> {
                    ("data-1.raw.bin", 65536, "0A43E6858977A39B861420FA31877030A0E683F1E25FFCF6A42098E6CB4C4948",
                        "data-1.aes.bin", 65552,
                        "2222C7B3D3D1896636DDC0642F8CC2F882D23CECCE1D2FEE7678B5B3587A3163"),
                    ("data-2.raw.bin", 13659, "8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF",
                        "data-2.aes.bin", 13664,
                        "E1223699AFBDFBB5252D7CCEA23A40BFCE8DD4834A53E52A75C778BEC3C72706")
                };

        [Fact]
        public void PimixCryptoStreamDecryptionReadBasicTest() {
            foreach (var (rawFile, rawSize, rawHash, encryptedFile, encryptedSize, encryptedHash) in data) {
                var transform = aesAlgorithm.CreateDecryptor();

                using var stream = new PimixCryptoStream(File.OpenRead(encryptedFile), transform,
                    rawSize, true);
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

        [Fact]
        public void PimixCryptoStreamDecryptionReadIncompleteEndTest() {
            foreach (var (rawFile, rawSize, rawHash, encryptedFile, encryptedSize, encryptedHash) in data) {
                var transform = aesAlgorithm.CreateDecryptor();

                using var stream = new PimixCryptoStream(File.OpenRead(encryptedFile), transform,
                    rawSize, true);
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

        [Fact]
        public void PimixCryptoStreamEncryptionReadBasicTest() {
            foreach (var (rawFile, rawSize, rawHash, encryptedFile, encryptedSize, encryptedHash) in data) {
                var transform = aesAlgorithm.CreateEncryptor();

                using var stream = new PimixCryptoStream(File.OpenRead(rawFile), transform,
                    encryptedSize, false);
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
