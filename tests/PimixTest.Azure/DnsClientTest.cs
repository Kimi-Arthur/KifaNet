using System;
using Pimix.Azure;
using Pimix.Configs;
using Xunit;

namespace PimixTest.Azure {
    public class DnsClientTest {
        [Fact]
        public void ShouldUpdateIp() {
            PimixConfigs.LoadFromSystemConfigs();

            var random = new Random();
            var ip = string.Join(".", random.Next(256), random.Next(256), random.Next(256), random.Next(256));
            new DnsClient().ReplaceIp("test", ip);
        }
    }
}
