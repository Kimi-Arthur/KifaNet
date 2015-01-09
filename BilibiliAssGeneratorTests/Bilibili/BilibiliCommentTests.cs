using System;
using BiliBiliAssGenerator.Bilibili;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BilibiliAssGeneratorTests.Bilibili
{
    [TestClass]
    public class BilibiliCommentTests
    {
        [TestMethod]
        public void TestBilibiliComment()
        {
            var comment = new BilibiliComment("163.708,1,25,16777215,1420312268,0,fd235204,731262841", "听不懂也能跟着笑～～～");
            Assert.AreEqual(TimeSpan.FromSeconds(163.708), comment.VideoTime);
            Assert.AreEqual("听不懂也能跟着笑～～～", comment.Text);
        }
    }
}
