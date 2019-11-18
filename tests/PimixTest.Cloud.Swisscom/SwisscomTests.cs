using System;
using Pimix.Cloud.Swisscom;
using Xunit;

namespace PimixTest.Cloud.Swisscom {
    public class SwisscomTests {
        [Fact]
        public void LoginTest() {
            var account = new SwisscomAccount {
                Username = "pimixserver@gmail.com",
                Password = "Pny3YQzV"
            };

            Assert.EndsWith("==", account.Token);
        }
    }
}
