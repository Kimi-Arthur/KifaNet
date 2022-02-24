using System;
using System.Linq;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliChatTests {
    [Fact]
    public void BasicTest() {
        var chat = new BilibiliChat {
            Cid = "2862733",
            Title = "测试标题"
        };
        Assert.Equal("2862733", chat.Cid);
        Assert.Equal(TimeSpan.Zero, chat.ChatOffset);
        Assert.Equal("测试标题", chat.Title);
        // Assert.Equal(TimeSpan.FromMilliseconds(5340000), chat.ChatLength);
        Assert.True(chat.Comments.Count() > 1000, "Comments count should be > 1000");
    }

    [Fact]
    public void WithOffsetTest() {
        var chat = new BilibiliChat {
            Cid = "2862733",
            Title = "测试标题"
        };

        var time = chat.Comments.ElementAt(1).VideoTime;

        var offset = TimeSpan.FromSeconds(100);
        chat.ChatOffset = offset;
        Assert.Equal(time.Add(offset), chat.Comments.ElementAt(1).VideoTime);

        chat.ChatOffset = TimeSpan.Zero;
        Assert.Equal(time, chat.Comments.ElementAt(1).VideoTime);
    }
}
