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
            AssDialogueTextElement e1 = "test1";
            Assert.AreEqual("test1", e1.GenerateAssText());

            AssDialogueTextElement e2 = new AssDialogueTextElement();
            Assert.AreEqual("", e2.GenerateAssText());
        }
    }
}
