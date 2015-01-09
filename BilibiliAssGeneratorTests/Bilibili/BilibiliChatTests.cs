using System;
using BiliBiliAssGenerator.Bilibili;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BilibiliAssGeneratorTests.Bilibili
{
    [TestClass]
    public class BilibiliChatTests
    {
        [TestMethod]
        public void BasicTest()
        {
            var chat = new BilibiliChat("2862733", "");
        }
    }
}
