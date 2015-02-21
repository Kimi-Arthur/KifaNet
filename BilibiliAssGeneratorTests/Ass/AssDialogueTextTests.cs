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

        [TestMethod]
        public void StyleTest()
        {
            var element = new AssDialogueTextElement("two2");
            element.Bold = false;
            element.Italic = true;
            element.Underline = true;
            element.StrikeOut = null;
            var text = new AssDialogueText(element);
            Assert.AreEqual(@"{\b0\i1\u1}two2", text.GenerateAssText());

            text.Alignment = AssAlignment.BottomCenter;
            Assert.AreEqual(@"{\an2}{\b0\i1\u1}two2", text.GenerateAssText());

            text.Alignment = null;
            Assert.AreEqual(@"{\b0\i1\u1}two2", text.GenerateAssText());
        }
    }
}
