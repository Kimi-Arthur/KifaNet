using Pimix.Subtitle.Ass;
using Xunit;

namespace PimixTest.Subtitle.Ass {
    public class AssLineTests {
        [Fact]
        public void BasicTest() {
            var line = new AssLine("MyKey", new[] {"item1", "item2", "item3"});
            Assert.Equal("MyKey: item1,item2,item3", line.ToString());
        }

        [Fact]
        public void MultiLineTest() {
            var line = new AssLine("MyKey", new[] {"ite\nm1", "it\rem2", "it\nem3"});
            Assert.Equal(@"MyKey: ite\nm1,it\nem2,it\nem3", line.ToString());
        }
    }
}
