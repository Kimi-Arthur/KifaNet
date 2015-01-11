using System;
using System.Drawing;
using BiliBiliAssGenerator.Ass;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BilibiliAssGeneratorTests.Ass
{
    [TestClass]
    public class AssExtensionsTests
    {
        [TestMethod]
        public void BoolTextTest()
        {
            Assert.AreEqual("-1", true.GenerateAssText());
            Assert.AreEqual("0", false.GenerateAssText());
        }

        [TestMethod]
        public void ColorTextTest()
        {
            Assert.AreEqual("&H3C141414", Color.FromArgb(0x3C141414).GenerateAssText());
        }

        [TestMethod]
        public void IntTextTest()
        {
            Assert.AreEqual("123", 123.GenerateAssText());
        }

        [TestMethod]
        public void StrTextTest()
        {
            Assert.AreEqual("abc", "abc".GenerateAssText());
        }
    }
}
