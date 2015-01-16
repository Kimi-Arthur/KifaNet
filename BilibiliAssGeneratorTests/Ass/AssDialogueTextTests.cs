using System;
using System.Collections.Generic;
using BilibiliAssGenerator.Ass;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BilibiliAssGeneratorTests.Ass
{
    [TestClass]
    public class AssDialogueTextTests
    {
        [TestMethod]
        public void BasicTest()
        {
            AssElement text = new AssDialogueText()
            {
                TextElements = new List<AssDialogueTextElement> { "one1" }
            };

            Assert.AreEqual("one1", text.GenerateAssText());
        }
    }
}
