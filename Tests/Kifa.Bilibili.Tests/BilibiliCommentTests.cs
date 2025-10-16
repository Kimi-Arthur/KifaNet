using System;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliCommentTests {
    [Fact]
    public void BasicTest() {
        var comment = new BilibiliComment("163.708,1,25,16777215,1420312268,0,fd235204,731262841",
            "听不懂也能跟着笑～～～");
        Assert.Equal(TimeSpan.FromSeconds(163.708), comment.VideoTime);
        Assert.Equal(BilibiliComment.ModeType.Normal, comment.Mode);
        Assert.Equal(25, comment.FontSize);
        Assert.Null(comment.TextColor);
        Assert.Equal(new DateTime(2015, 1, 3, 19, 11, 8), comment.PostTime);
        Assert.Equal(BilibiliComment.PoolType.Normal, comment.Pool);
        Assert.Equal(4246950404L, comment.UserId);
        Assert.Equal(731262841, comment.CommentId);
        Assert.Equal("听不懂也能跟着笑～～～", comment.Text);
    }

    [Fact]
    public void StructCopyTest() {
        var comment1 = new BilibiliComment("163.708,1,25,16777215,1420312268,0,fd235204,731262841",
            "听不懂也能跟着笑～～～");
        var comment2 = comment1;
        comment2.Text = "abc";
        Assert.Equal("听不懂也能跟着笑～～～", comment1.Text);
    }

    [Fact]
    public void WithOffsetTest() {
        var comment = new BilibiliComment("163.708,1,25,16777215,1420312268,0,fd235204,731262841",
            "听不懂也能跟着笑～～～");
        Assert.Equal(TimeSpan.FromSeconds(263.708),
            comment.WithOffset(TimeSpan.FromSeconds(100)).VideoTime);
        // Test original data
        Assert.Equal(TimeSpan.FromSeconds(163.708), comment.VideoTime);
    }
}
