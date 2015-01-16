using System;
using BilibiliAssGenerator.Ass;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BilibiliAssGeneratorTests.Ass
{
    [TestClass]
    public class AssDialogueTextElementTests
    {
        [TestMethod]
        public void NormalElementTest()
        {
            AssDialogueTextNormalElement e1 = "test1";
            Assert.AreEqual("test1", e1.GenerateAssText());

            AssDialogueTextNormalElement e2 = new AssDialogueTextNormalElement();
            Assert.AreEqual("", e2.GenerateAssText());
        }
    }
}
