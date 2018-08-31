using System;
using BilibiliAssGenerator.Ass;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BilibiliAssGeneratorTests.Ass
{
    [TestClass]
    public class AssLineTests
    {
        [TestMethod]
        public void BasicTest()
        {
            AssElement line = new AssLine("MyKey", new string[] { "item1", "item2", "item3" });
            Assert.AreEqual("MyKey: item1,item2,item3", line.GenerateAssText());
        }

        [TestMethod]
        public void MultiLineTest()
        {
            AssElement line = new AssLine("MyKey", new string[] { "ite\nm1", "it\rem2", "it\r\nem3" });
            Assert.AreEqual(@"MyKey: ite\nm1,it\nem2,it\nem3", line.GenerateAssText());
        }
    }
}
