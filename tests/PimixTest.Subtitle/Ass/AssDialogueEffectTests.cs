using System;
using Pimix.Subtitle.Ass;
using Xunit;

namespace PimixTest.Subtitle.Ass {
    public class AssDialogueEffectTests {
        [Fact]
        public void BannerEffectBasicTest() {
            var effect = new AssDialogueBannerEffect();
            Assert.Equal("Banner;0;0;0", effect.ToString());
            effect = new AssDialogueBannerEffect {
                Delay = 12,
                FadeAwayWidth = 2,
                LeftToRight = AssDialogueBannerEffect.LeftToRightType.LeftToRight
            };
            Assert.Equal("Banner;12;1;2", effect.ToString());
        }

        [Fact]
        public void BannerEffectRangeTest() {
            var effect = new AssDialogueBannerEffect();

            foreach (var d in new[] {
                -1, 101, 1024
            }) {
                Assert.Equal("Delay", Assert
                    .Throws<ArgumentOutOfRangeException>(() => effect.Delay = d)
                    .ParamName);
            }
        }
    }
}
