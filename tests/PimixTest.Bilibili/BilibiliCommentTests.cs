using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Bilibili;
using Pimix.Subtitle.Ass;

namespace PimixTest.Bilibili {
    [TestClass]
    public class BilibiliCommentTests {
        [TestMethod]
        public void BasicTest() {
            var comment =
                new BilibiliComment("163.708,1,25,16777215,1420312268,0,fd235204,731262841",
                    "听不懂也能跟着笑～～～");
            Assert.AreEqual(TimeSpan.FromSeconds(163.708), comment.VideoTime);
            Assert.AreEqual(BilibiliComment.ModeType.Normal, comment.Mode);
            Assert.AreEqual(25, comment.FontSize);
            Assert.AreEqual(null, comment.TextColor);
            Assert.AreEqual(new DateTime(2015, 1, 3, 19, 11, 8), comment.PostTime);
            Assert.AreEqual(BilibiliComment.PoolType.Normal, comment.Pool);
            Assert.AreEqual(4246950404L, comment.UserId);
            Assert.AreEqual(731262841, comment.CommentId);
            Assert.AreEqual("听不懂也能跟着笑～～～", comment.Text);
        }

        [TestMethod]
        public void StructCopyTest() {
            var comment1 =
                new BilibiliComment("163.708,1,25,16777215,1420312268,0,fd235204,731262841",
                    "听不懂也能跟着笑～～～");
            var comment2 = comment1;
            comment2.Text = "abc";
            Assert.AreEqual("听不懂也能跟着笑～～～", comment1.Text);
        }

        [TestMethod]
        public void WithOffsetTest() {
            var comment =
                new BilibiliComment("163.708,1,25,16777215,1420312268,0,fd235204,731262841",
                    "听不懂也能跟着笑～～～");
            Assert.AreEqual(TimeSpan.FromSeconds(263.708),
                comment.WithOffset(TimeSpan.FromSeconds(100)).VideoTime);
            // Test original data
            Assert.AreEqual(TimeSpan.FromSeconds(163.708), comment.VideoTime);
        }
    }
}
