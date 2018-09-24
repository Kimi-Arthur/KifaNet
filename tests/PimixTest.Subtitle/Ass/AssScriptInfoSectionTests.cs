using Pimix.Subtitle.Ass;
using Xunit;

namespace PimixTest.Subtitle.Ass {
    public class AssScriptInfoSectionTests {
        [Fact]
        public void BasicTest() {
            var section = new AssScriptInfoSection();
            Assert.StartsWith("[Script Info]\r\nScript Type: V4.00+\r\n", section.ToString());
        }

        [Fact]
        public void WithValueTest() {
            var section = new AssScriptInfoSection
                {OriginalScript = "Kimi", Title = "Great!", ScriptType = "Special type"};
            Assert.StartsWith(
                "[Script Info]\r\nTitle: Great!\r\nOriginal Script: Kimi\r\nScript Type: Special type\r\n",
                section.ToString());
        }
    }
}
