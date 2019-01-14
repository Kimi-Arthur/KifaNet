using System.Collections.Generic;
using Pimix.Subtitle.Ass;
using Xunit;

namespace PimixTest.Subtitle.Ass {
    public class AssDialogueTextTests {
        [Fact]
        public void BasicTest() {
            var text = new AssDialogueText {
                TextElements = new List<AssDialogueTextElement> {
                    new AssDialogueRawTextElement {
                        Content = "one1"
                    }
                }
            };

            Assert.Equal("one1", text.ToString());
        }

        [Fact]
        public void StyleTest() {
            var controlElement = new AssDialogueControlTextElement {
                Elements = new List<AssControlElement> {
                    new BoldStyle {Value = false},
                    new ItalicStyle(),
                    new UnderlineStyle()
                }
            };

            var text = new AssDialogueText {
                TextElements = new List<AssDialogueTextElement> {
                    controlElement,
                    new AssDialogueRawTextElement {
                        Content = "two2"
                    }
                }
            };

            Assert.Equal(@"{\b0\i1\u1}two2", text.ToString());

            controlElement.Elements.Insert(0,
                new AlignmentStyle {Value = AssAlignment.BottomCenter});
            Assert.Equal(@"{\an2\b0\i1\u1}two2", text.ToString());

            controlElement.Elements.RemoveAt(0);
            Assert.Equal(@"{\b0\i1\u1}two2", text.ToString());
        }
    }
}
