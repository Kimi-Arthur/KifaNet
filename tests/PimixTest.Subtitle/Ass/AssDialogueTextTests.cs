using System.Collections.Generic;
using Pimix.Subtitle.Ass;
using Xunit;

namespace PimixTest.Subtitle.Ass {
    public class AssDialogueTextTests {
        [Fact]
        public void BasicTest() {
            var text = new AssDialogueText {
                TextElements = new List<AssDialogueTextElement> {"one1"}
            };

            Assert.Equal("one1", text.ToString());
        }

        [Fact]
        public void StyleTest() {
            var element = new AssDialogueTextElement {
                Content = "two2",
                Bold = false,
                Italic = true,
                Underline = true,
                StrikeOut = null
            };
            var text = new AssDialogueText(element);
            Assert.Equal(@"{\b0\i1\u1}two2", text.ToString());

            text.Alignment = AssAlignment.BottomCenter;
            Assert.Equal(@"{\an2}{\b0\i1\u1}two2", text.ToString());

            text.Alignment = null;
            Assert.Equal(@"{\b0\i1\u1}two2", text.ToString());
        }
    }
}
