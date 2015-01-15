using System;
using BilibiliAssGenerator.Ass;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BilibiliAssGeneratorTests.Ass
{
    [TestClass]
    public class AssScriptInfoSectionTests
    {
        [TestMethod]
        public void BasicTest()
        {
            AssElement section = new AssScriptInfoSection();
            Assert.AreEqual("[Script Info]\r\nScript Type: V4.00+", section.GenerateAssText());
        }

        [TestMethod]
        public void WithValueTest()
        {
            AssElement section = new AssScriptInfoSection() { OriginalScript = "Kimi", Title = "Great!", ScriptType = "Special type" };
            Assert.AreEqual(
                "[Script Info]\r\nTitle: Great!\r\nOriginal Script: Kimi\r\nScript Type: Special type",
                section.GenerateAssText());
        }
    }
}
