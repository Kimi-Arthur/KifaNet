using System.Drawing;
using Pimix.Subtitle.Ass;
using Xunit;

namespace PimixTest.Subtitle.Ass {
    public class AssDialogueTextElementTests {
        [Fact]
        public void NormalElementTest() {
            AssDialogueTextElement e1 = "test1";
            Assert.Equal("test1", e1.ToString());
        }

        [Fact]
        public void EmptyElementTest() {
            var e2 = new AssDialogueTextElement();
            Assert.Equal("", e2.ToString());
        }

        [Fact]
        public void StyleTest() {
            AssDialogueTextElement e3 = "test3";
            e3.BackColour = Color.AliceBlue;
            e3.Bold = true;
            e3.StrikeOut = false;
            e3.Italic = null;
            e3.FontRotationX = 12;
            Assert.Equal(@"{\b1\s0\frx12\4a&H00&\4c&HFFF8F0&}test3", e3.ToString());
        }
    }
}
