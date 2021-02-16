using Kifa.Configs;
using NUnit.Framework;

namespace Kifa.Memrise.Tests {
    public class MemriseClientTests {
        [SetUp]
        public void Setup() {
            KifaConfigs.LoadFromSystemConfigs();
        }

        [Test]
        public void Test1() {
            KifaConfigs.LoadFromSystemConfigs();
            var client = new MemriseClient();
            client.LoadCourse();
        }
    }
}
