using System;
using System.Linq;
using BilibiliAssGenerator.Bilibili;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BilibiliAssGeneratorTests.Bilibili
{
    [TestClass]
    public class BilibiliChatTests
    {
        [TestMethod]
        public void BasicTest()
        {
            var chat = new BilibiliChat("2862733", "测试标题");
            Assert.AreEqual("2862733", chat.Cid);
            Assert.AreEqual(TimeSpan.Zero, chat.ChatOffset);
            Assert.AreEqual("测试标题", chat.Title);
            Assert.AreEqual(TimeSpan.FromMilliseconds(5340000), chat.ChatLength);
            Assert.IsTrue(chat.Comments.Count() > 1000, "Comments count should be > 1000");
            Assert.AreEqual(
                new BilibiliComment("163.708,1,25,16777215,1420312268,0,fd235204,731262841", "听不懂也能跟着笑～～～"),
                chat.Comments.ElementAt(1));
        }

        [TestMethod]
        public void WithOffsetTest()
        {
            var chat = new BilibiliChat("2862733", "测试标题");

            Assert.AreEqual(TimeSpan.FromSeconds(163.708), chat.Comments.ElementAt(1).VideoTime);

            chat.ChatOffset = TimeSpan.FromSeconds(100);
            Assert.AreEqual(TimeSpan.FromSeconds(163.708 + 100), chat.Comments.ElementAt(1).VideoTime);

            chat.ChatOffset = TimeSpan.Zero;
            Assert.AreEqual(TimeSpan.FromSeconds(163.708), chat.Comments.ElementAt(1).VideoTime);
        }
    }
}
