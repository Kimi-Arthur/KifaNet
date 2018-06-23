using Pimix.Cloud.GoogleDrive;
using Pimix.IO;
using Xunit;

namespace PimixTest.Cloud.GoogleDrive {
    public class GoogleDriveStorageClientTests {
        public static string PimixServerApiAddress { get; set; } = "http://www.pimix.tk/api";

        public GoogleDriveStorageClientTests() {
            GoogleDriveConfig.PimixServerApiAddress = PimixServerApiAddress;
            GoogleDriveStorageClient.Config = GoogleDriveConfig.Get("default");
        }

        const string FileSHA256 =
            "68EB5DFB2935868A17EEDDB315FBF6682243D29C1C1A20CC06BD25627F596285";

        [Fact]
        public void DownloadTest() {
            var client = GetStorageClient();

            using (var s = client.OpenRead("/Test/2010-11-25.bin")) {
                Assert.Equal(FileSHA256,
                    FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
                
                // Test again for seekness.
                Assert.Equal(FileSHA256,
                    FileInformation.GetInformation(s, FileProperties.SHA256).SHA256);
            }
        }

        static GoogleDriveStorageClient GetStorageClient()
            => new GoogleDriveStorageClient() {AccountId = "good"};
    }
}
