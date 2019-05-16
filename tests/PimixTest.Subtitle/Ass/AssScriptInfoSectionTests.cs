using Pimix.Subtitle.Ass;
using Xunit;

namespace PimixTest.Subtitle.Ass {
    public class AssScriptInfoSectionTests {
        [Fact]
        public void BasicTest() {
            var section = new AssScriptInfoSection();
            Assert.StartsWith("[Script Info]\nScript Type: V4.00+\n", section.ToString());
        }

        [Fact]
        public void WithValueTest() {
            var section = new AssScriptInfoSection {
                OriginalScript = "Kimi",
                Title = "Great!",
                ScriptType = "Special type"
            };
            Assert.StartsWith("[Script Info]\nTitle: Great!\nOriginal Script: Kimi\nScript Type: Special type\n",
                section.ToString());
        }
    }
}
