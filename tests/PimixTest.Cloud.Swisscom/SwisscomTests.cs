using Pimix.Cloud.Swisscom;
using Pimix.Configs;
using Pimix.IO;
using Xunit;

namespace PimixTest.Cloud.Swisscom {
    public class SwisscomTests {
        const string FileSHA256 =
            "68EB5DFB2935868A17EEDDB315FBF6682243D29C1C1A20CC06BD25627F596285";

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
        public void DownloadTest() {
            var client = GetStorageClient();

            using var s = client.OpenRead("/Test/2010-11-25.bin");
            Assert.Equal(FileSHA256,
                FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);

            // Test again for seekness.
            Assert.Equal(FileSHA256,
                FileInformation.GetInformation(s, FileProperties.Sha256).Sha256);
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
