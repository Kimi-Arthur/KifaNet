using System;
using BilibiliAssGenerator.Ass;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BilibiliAssGeneratorTests.Ass
{
    [TestClass]
    public class AssDialogueEffectTests
    {
        [TestMethod]
        public void BannerEffectBasicTest()
        {
            var effect = new AssDialogueBannerEffect();
            Assert.AreEqual("Banner;0;0;0", effect.GenerateAssText());
            effect = new AssDialogueBannerEffect()
            {
                Delay = 12,
                FadeAwayWidth = 2,
                LeftToRight = AssDialogueBannerEffect.LeftToRightType.LeftToRight
            };
            Assert.AreEqual("Banner;12;1;2", effect.GenerateAssText());
        }

        [TestMethod]
        public void BannerEffectRangeTest()
        {
            var effect = new AssDialogueBannerEffect();
            Assert.IsInstanceOfType(Utils.GetException(() => effect.Delay = -1), typeof(ArgumentOutOfRangeException), "Property delay");
            Assert.IsInstanceOfType(Utils.GetException(() => effect.Delay = 101), typeof(ArgumentOutOfRangeException), "Property delay");
            Assert.IsInstanceOfType(Utils.GetException(() => effect.Delay = 1024), typeof(ArgumentOutOfRangeException), "Property delay");
        }
    }
}
