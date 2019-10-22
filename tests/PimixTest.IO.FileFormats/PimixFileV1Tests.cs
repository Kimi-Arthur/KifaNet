using System;
using System.IO;
using Pimix.IO;
using Pimix.IO.FileFormats;
using Xunit;

namespace PimixTest.IO.FileFormats {
    public class PimixFileV1Tests {
        const string EncryptionKey = "C7C37D56DD70FD6258BDD01AED083C88432EC27536DF9328D6329382183DB795";

        [Theory]
        [InlineData(100)]
        [InlineData((64 << 20) + 100)]
        [InlineData(64 << 20)]
        [InlineData((64 << 20) - 16)]
        [InlineData((64 << 20) + 16)]
        [InlineData((64 << 20) - 8)]
        [InlineData((64 << 20) - 1)]
        [InlineData((64 << 20) + 1)]
        public void RoundTripTest(int length) {
            var data = new byte[length];
            new Random().NextBytes(data);
            var format = new PimixFileV1Format();

            using var ms = new MemoryStream(data);
            using var encrypted = new MemoryStream();
            using var encryptionStream = format.GetEncodeStream(ms,
                new FileInformation {
                    EncryptionKey = EncryptionKey,
                    Size = length
                });
            encryptionStream.CopyTo(encrypted);
            using var output =
                format.GetDecodeStream(encrypted, EncryptionKey);
            var fs1 = FileInformation.GetInformation(ms,
                FileProperties.Size | FileProperties.Sha256);
            var fs2 = FileInformation.GetInformation(output,
                FileProperties.Size | FileProperties.Sha256);
            Assert.Equal(fs1.Size, fs2.Size);
            Assert.Equal(fs1.Sha256, fs2.Sha256);
        }
    }
}
