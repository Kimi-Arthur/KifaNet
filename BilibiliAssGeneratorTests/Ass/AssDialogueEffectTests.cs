using System;
using BilibiliAssGenerator.Ass;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BilibiliAssGeneratorTests.Ass
{
    [TestClass]
    public class AssDialogueEffectTests
    {
        [TestMethod]
        public void BasicTest()
        {
            var effect = new AssDialogueBannerEffect();
            Assert.AreEqual("Banner;0;0;0", effect.GenerateAssText());
        }
    }
}
