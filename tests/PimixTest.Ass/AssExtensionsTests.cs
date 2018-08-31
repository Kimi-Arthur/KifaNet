using System;
using System.Drawing;
using Pimix.Ass;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PimixTest.Ass
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
        public void DoubleTextTest()
        {
            Assert.AreEqual("0.12", 0.123.GenerateAssText());
            Assert.AreEqual("12.12", 12.123.GenerateAssText());
            Assert.AreEqual("-0.12", (-0.123).GenerateAssText());
            Assert.AreEqual("123.00", 123.0.GenerateAssText());
            Assert.AreEqual("0.12", 0.117.GenerateAssText());
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

        [TestMethod]
        public void TimeSpanTextTest()
        {
            Assert.AreEqual("1:06:41.23", TimeSpan.FromSeconds(4001.234).GenerateAssText());
        }
    }
}
