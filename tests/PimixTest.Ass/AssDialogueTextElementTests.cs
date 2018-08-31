using System;
using Pimix.Ass;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PimixTest.Ass
{
    [TestClass]
    public class AssDialogueTextElementTests
    {
        [TestMethod]
        public void NormalElementTest()
        {
            AssDialogueTextElement e1 = "test1";
            Assert.AreEqual("test1", e1.GenerateAssText());
        }

        [TestMethod]
        public void EmptyElementTest()
        {
            AssDialogueTextElement e2 = new AssDialogueTextElement();
            Assert.AreEqual("", e2.GenerateAssText());
        }

        [TestMethod]
        public void StyleTest()
        {
            AssDialogueTextElement e3 = "test3";
            e3.BackColour = System.Drawing.Color.AliceBlue;
            e3.Bold = true;
            e3.StrikeOut = false;
            e3.Italic = null;
            e3.FontRotationX = 12;
            Assert.AreEqual(@"{\b1\s0\frx12\4a&HFF&\4c&HFFF8F0&}test3", e3.GenerateAssText());
        }
    }
}
