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
            Assert.AreEqual("[Script Info]\nScript Type: V4.00+", section.GenerateText());
        }

        [TestMethod]
        public void WithValueTest()
        {
            AssElement section = new AssScriptInfoSection() { OriginalScript = "Kimi", Title = "Great!", ScriptType = "Special type" };
            Assert.AreEqual(
                "[Script Info]\nTitle: Great!\nOriginal Script: Kimi\nScript Type: Special type",
                section.GenerateText());
        }
    }
}
