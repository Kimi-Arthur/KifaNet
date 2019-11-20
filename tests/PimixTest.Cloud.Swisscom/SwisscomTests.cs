using System;
using System.IO;
using System.Threading;
using Pimix.Cloud.Swisscom;
using Pimix.Configs;
using Pimix.IO;
using Xunit;

namespace PimixTest.Cloud.Swisscom {
    public class SwisscomTests {
        const string FileSHA256 =
            "68EB5DFB2935868A17EEDDB315FBF6682243D29C1C1A20CC06BD25627F596285";

        const string BigFileSHA256 =
            "C15129F8F953AF57948FBC05863C42E16A8362BD5AEC9F88C566998D1CED723A";

        public SwisscomTests() {
            PimixConfigs.LoadFromSystemConfigs();
        }

        [Fact]
        public void LoginTest() {
            var account = GetStorageClient().Account;

            Assert.EndsWith("==", account.Token);
        }

        [Fact]
        public void LengthTest() {
            var client = GetStorageClient();

            Assert.Equal(1 << 20, client.Length("/Test/2010-11-25.bin"));
        }

        [Fact]
        public void ExistsTest() {
            var client = GetStorageClient();

            Assert.True(client.Exists("/Test/2010-11-25.bin"));
            Assert.False(client.Exists("/Test/2015-11-25.bin"));
            Assert.False(client.Exists("/Test/NoFolder/2015-11-25.bin"));
        }

        [Fact]
        public void DownloadTest() {
            var client = GetStorageClient();

            using var s = client.OpenRead("/Test/2010-11-25.bin");
            Assert.Equal(FileSHA256,
                FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);

            // Test again for seekness.
            Assert.Equal(FileSHA256,
                FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
        }

        [Fact]
        public void UploadTest() {
            var client = GetStorageClient();
            var data = new byte[34 << 20];
            File.OpenRead("data.bin").Read(data, 0, 1 << 20);
            for (var i = 1; i < 34; ++i) {
                Array.Copy(data, 0, data, i << 20, 1 << 20);
            }

            var dataStream = new MemoryStream(data);

            client.Write("/Test/big.bin", dataStream);

            Thread.Sleep(TimeSpan.FromSeconds(1));

            using (var s = client.OpenRead("/Test/big.bin")) {
                Assert.Equal(BigFileSHA256,
                    FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
            }

            client.Delete("/Test/big.bin");
        }

        [Fact]
        public void CopyTest() {
            var client = GetStorageClient();

            client.Copy("/Test/2010-11-25.bin", "/Test/2010-11-25.bin_bak");
            using (var s = client.OpenRead("/Test/2010-11-25.bin_bak")) {
                Assert.Equal(FileSHA256,
                    FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
            }

            client.Delete("/Test/2010-11-25.bin_bak");
        }

        [Fact]
        public void MoveTest() {
            var client = GetStorageClient();

            client.Copy("/Test/2010-11-25.bin", "/Test/2010-11-25.bin_1");
            Assert.True(client.Exists("/Test/2010-11-25.bin_1"));
            Assert.False(client.Exists("/Test/2010-11-25.bin_2"));

            client.Move("/Test/2010-11-25.bin_1", "/Test/2010-11-25.bin_2");
            Assert.False(client.Exists("/Test/2010-11-25.bin_1"));
            Assert.True(client.Exists("/Test/2010-11-25.bin_2"));

            using (var s = client.OpenRead("/Test/2010-11-25.bin_2")) {
                Assert.Equal(FileSHA256,
                    FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
            }

            client.Delete("/Test/2010-11-25.bin_2");
        }

        [Fact]
        public void QuotaTest() {
            var client = GetStorageClient();

            var (total, used) = client.GetQuota();
            Assert.Equal(10737418240, total);
            Assert.NotEqual(0, used);
        }

        static SwisscomStorageClient GetStorageClient()
            => new SwisscomStorageClient {
                Account = new SwisscomAccount {
                    Username = "pimixserver@gmail.com",
                    Password = "Pny3YQzV"
                }
            };
    }
}
