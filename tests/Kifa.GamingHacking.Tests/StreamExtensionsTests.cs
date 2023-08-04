using System.IO;
using System.Text;
using Kifa.GameHacking;
using Xunit;

namespace Kifa.GamingHacking.Tests;

public class StreamExtensionsTests {
    [Fact]
    public void GetStringTest() {
        var input = "I'm dead beef\0good"u8.ToArray();
        var stream = new MemoryStream(input);
        Assert.Equal("I'm dead beef", stream.GetString(13));
        stream.Reset();
        Assert.Equal("I'm dead b", stream.GetString(10));
        Assert.Equal("eef\0goo", stream.GetString(7));
        Assert.Throws<EndOfStreamException>(() => stream.GetString(3));

        stream.Reset();
        stream.AssertStrings("I'm", " dead", " beef\0g");
        Assert.Throws<DecodeException>(() => stream.AssertStrings("g"));
    }

    [Fact]
    public void GetStringWithEncodingTest() {
        var input = "中文abc"u8.ToArray();
        var stream = new MemoryStream(input);
        Assert.NotEqual("中文abc", stream.GetString(9));
        stream.Reset();
        Assert.Equal("中文abc", stream.GetString(9, Encoding.UTF8));
        Assert.Throws<EndOfStreamException>(() => stream.GetString(1));

        stream.Reset();
        stream.AssertStrings(Encoding.UTF8, "中文abc");
    }

    [Fact]
    public void GetNullEndedStringTest() {
        var input = "I'm dead beef\0good"u8.ToArray();
        var stream = new MemoryStream(input);
        Assert.Equal("I'm dead beef", stream.GetNullEndedString());
        Assert.Equal("good", stream.GetNullEndedString());
        Assert.Throws<EndOfStreamException>(() => stream.GetString(3));

        stream.Reset();
        stream.AssertNullEndedStrings("I'm dead beef", "good");

        stream.Reset();
        Assert.Throws<DecodeException>(() => stream.AssertNullEndedStrings("I'm"));
    }

    [Fact]
    public void GetNumberTests() {
        var input = new byte[] { 0x01, 0x23, 0x34, 0xAF };
        var stream = new MemoryStream(input);
        Assert.Equal(0x2301, stream.GetNumber<short>());
        Assert.Equal(0x34AF, stream.GetNumber<short>(bigEndian: true));

        stream.Reset();
        Assert.Equal(0x01, stream.GetNumber<byte>());
        Assert.Equal(0x23, stream.GetNumber<byte>(bigEndian: true));

        stream.Reset();
        Assert.Equal(-0x50CBDCFF, stream.GetNumber<int>());

        stream.Reset();
        Assert.Equal(0x12334AF, stream.GetNumber<int>(bigEndian: true));

        stream.Reset();
        Assert.Equal(0xAF342301u, stream.GetNumber<uint>());

        stream.Reset();
        Assert.Equal(0x12334AFu, stream.GetNumber<uint>(bigEndian: true));

        stream.Reset();
        stream.AssertNumbers(0xAF342301u);

        stream.Reset();
        stream.AssertNumbers<short>(bigEndian: true, 0x123, 0x34AF);
    }
}
