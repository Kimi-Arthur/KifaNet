using System;
using Kifa.Configs;
using Xunit;

namespace Kifa.Azure.Tests {
    public class DnsClientTest {
        [Fact]
        public void ShouldUpdateIp() {
            KifaConfigs.LoadFromSystemConfigs();

            var random = new Random();
            var ip = string.Join(".", random.Next(256), random.Next(256), random.Next(256), random.Next(256));
            new DnsClient().ReplaceIp("test", ip);
        }
    }
}
