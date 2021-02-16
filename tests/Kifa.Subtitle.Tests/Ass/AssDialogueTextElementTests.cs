using System.Drawing;
using Kifa.Subtitle.Ass;
using Xunit;

namespace Kifa.Subtitle.Tests.Ass {
    public class AssDialogueTextElementTests {
        [Fact]
        public void NormalElementTest() {
            var e1 = new AssDialogueRawTextElement {
                Content = "test1"
            };
            Assert.Equal("test1", e1.ToString());
        }

        [Fact]
        public void StyleTest() {
            var e2 = new AssDialogueControlTextElement();
            e2.Elements.Add(new BackColourStyle {
                Value = Color.AliceBlue
            });
            e2.Elements.Add(new BoldStyle());
            e2.Elements.Add(new StrikeOutStyle {
                Value = false
            });
            e2.Elements.Add(new FontRotationXStyle {
                Value = 12
            });
            Assert.Equal(@"{\4c&HFFF8F0&\b1\s0\frx12}", e2.ToString());
        }
    }
}
