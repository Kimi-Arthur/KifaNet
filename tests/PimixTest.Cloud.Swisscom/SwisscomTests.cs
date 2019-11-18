using System;
using Pimix.Cloud.Swisscom;
using Pimix.Configs;
using Xunit;

namespace PimixTest.Cloud.Swisscom {
    public class SwisscomTests {
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

        static SwisscomStorageClient GetStorageClient()
            => new SwisscomStorageClient {
                Account = new SwisscomAccount {
                    Username = "pimixserver@gmail.com",
                    Password = "Pny3YQzV"
                }
            };
    }
}
