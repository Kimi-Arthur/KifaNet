using System;
using Pimix.Ass;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PimixTest.Ass {
    [TestClass]
    public class AssDialogueEffectTests {
        [TestMethod]
        public void BannerEffectBasicTest() {
            var effect = new AssDialogueBannerEffect();
            Assert.AreEqual("Banner;0;0;0", effect.GenerateAssText());
            effect = new AssDialogueBannerEffect() {
                Delay = 12,
                FadeAwayWidth = 2,
                LeftToRight = AssDialogueBannerEffect.LeftToRightType.LeftToRight
            };
            Assert.AreEqual("Banner;12;1;2", effect.GenerateAssText());
        }

        [TestMethod]
        public void BannerEffectRangeTest() {
            var effect = new AssDialogueBannerEffect();

            foreach (var d in new[] {-1, 101, 1024}) {
                Assert.AreEqual("Delay", Assert
                    .ThrowsException<ArgumentOutOfRangeException>(() => effect.Delay = d)
                    .ParamName);
            }
        }
    }
}
